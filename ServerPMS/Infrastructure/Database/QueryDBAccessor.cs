using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using ServerPMS.Abstractions.Infrastructure.Config;
using ServerPMS.Abstractions.Infrastructure.Database;
using Microsoft.Extensions.Hosting;
using System.Threading;

namespace ServerPMS.Infrastructure.Database
{
    public class QueryDBAccessor : BackgroundService, IQueryDBAccessor, IDisposable //wait for the query to return
    {
        private string db;
        private SqliteConnection connection;
        private readonly BlockingCollection<object> _queryQueue = new();
        private Task mainTask;

        private bool disposeRequested = false;

        private CancellationTokenSource cts;

        public bool IsRunning => _isRunning;
        private volatile bool _isRunning;

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

            _isRunning = false;

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
            Logger.LogInformation($"QDBA - Worker loop starting. (Thread: {Thread.CurrentThread.ManagedThreadId} )");
            try
            {
                foreach (var obj in _queryQueue.GetConsumingEnumerable(cts.Token))
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
                using var reader = await cmd.ExecuteReaderAsync(cts.Token);
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
                using var reader = await cmd.ExecuteReaderAsync(cts.Token);
                var result = request.Parser(reader);
                request.CompletionSource.SetResult(result);
            }
            catch (Exception ex)
            {
                request.CompletionSource.SetException(ex);
            }
        }

        // Call this to shutdown gracefully
        public override void Dispose()
        {
            connection.Dispose();
            _queryQueue.Dispose();
            base.Dispose();
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

            try {

                _isRunning = true;

                mainTask = Task.Factory.StartNew(
                    WorkerLoopAsync,
                    stoppingToken,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
                await mainTask;
            }
            finally
            {
                _isRunning = false;
            }
        }


        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _queryQueue.CompleteAdding();

            if (mainTask != null)
            {
                await mainTask;
            }

            connection.Close();

            await base.StopAsync(cancellationToken);

        }
    }
}

