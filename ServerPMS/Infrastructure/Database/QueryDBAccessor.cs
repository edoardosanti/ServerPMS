using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using ServerPMS.Abstractions.Infrastructure.Config;
using ServerPMS.Abstractions.Infrastructure.Database;

namespace ServerPMS.Infrastructure.Database
{
    public class QueryDBAccessor : IQueryDBAccessor, IDisposable //wait for the query to return
    {
        private string db;
        private SqliteConnection connection;
        private readonly BlockingCollection<object> _queryQueue = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly Task workerTask;

        private bool disposeRequested = false;


        private readonly IGlobalConfigManager GlobalConfig;
        private readonly ILogger<QueryDBAccessor> Logger;

        public QueryDBAccessor(IGlobalConfigManager configManager,ILogger<QueryDBAccessor> logger)
        {

            GlobalConfig = configManager;
            Logger = logger;

            Logger.LogInformation("QDBA - Starting.");

            db = GlobalConfig.GlobalRAMConfig.Database.FilePath;
            connection = new SqliteConnection(string.Format("Data Source={0};Mode=ReadWrite;", db));
            connection.Open();

            Logger.LogInformation("QDBA - Worker loop starting.");
            workerTask = Task.Run(WorkerLoopAsync);

        }

        // Enqueue a query and get a Task<T> for the result
        public Task<T> QueryAsync<T>(string sql, Func<DbDataReader, T> parser)
        {
            Logger.LogInformation("QDBA - New query enqueued: {0}", sql);
            var request = new QueryRequest<T> { Sql = sql, Parser = parser };
            _queryQueue.Add(request);
            return request.CompletionSource.Task;
        }

        private async Task WorkerLoopAsync()
        {
            try
            {
                foreach (var obj in _queryQueue.GetConsumingEnumerable(_cts.Token))
                {
                    if (obj is QueryRequest<object> genericRequest)
                    {
                        // This cast happens because BlockingCollection is object-based;
                        // we can safely cast to dynamic and invoke HandleQuery
                        await HandleQueryAsync(genericRequest);
                    }
                    else
                    {
                        // Use reflection to call generic method
                        var type = obj.GetType();
                        var method = typeof(QueryDBAccessor).GetMethod(nameof(HandleQueryGenericAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var genericMethod = method!.MakeGenericMethod(type.GetGenericArguments());
                        await (Task)genericMethod.Invoke(this, new[] { obj })!;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine("QDBA Worker Error: " + ex);
            }
        }

        private async Task HandleQueryAsync(QueryRequest<object> request)
        {
            try
            {
                Logger.LogInformation("QDBA - Executing SQL: {0}", request.Sql);
                using var cmd = connection.CreateCommand();
                cmd.CommandText = request.Sql;
                using var reader = await cmd.ExecuteReaderAsync(_cts.Token);
                var result = request.Parser(reader);
                request.CompletionSource.SetResult(result);
            }
            catch (Exception ex)
            {
                request.CompletionSource.SetException(ex);
            }
        }

        // Generic handler invoked via reflection
        private async Task HandleQueryGenericAsync<T>(QueryRequest<T> request)
        {
            try
            {
                Logger.LogInformation("QDBA - Executing SQL: {0}", request.Sql);
                using var cmd = connection.CreateCommand();
                cmd.CommandText = request.Sql;
                using var reader = await cmd.ExecuteReaderAsync(_cts.Token);
                var result = request.Parser(reader);
                request.CompletionSource.SetResult(result);
            }
            catch (Exception ex)
            {
                request.CompletionSource.SetException(ex);
            }
        }

        // Call this to shutdown gracefully
        public void Dispose()
        {
            Logger.LogInformation("QDBA - Shutting Down");
            _cts.Cancel();
            _queryQueue.CompleteAdding();
            workerTask.Wait();

            connection.Close();
            connection.Dispose();
            _cts.Dispose();
            _queryQueue.Dispose();
        }
    }
}

