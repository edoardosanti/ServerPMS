// PMS Project V1.0
// LSData - all rights reserved
// DBAccessor.cs
//
//
using System;
using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using DocumentFormat.OpenXml.Office2016.Excel;

namespace ServerPMS
{

    // Represents a query task with a typed result
    public class QueryRequest<T>
    {
        public string Sql { get; set; } = string.Empty;
        public Func<DbDataReader, T> Parser { get; set; } = default!;
        public TaskCompletionSource<T> CompletionSource { get; } = new();
    }

    public class CommandRequest
    {
        public string Sql { get; set; } = string.Empty;
        public TaskCompletionSource CompletionSource { get; } = new();
    }

    public class QueryDBAccessor //wait for the query to return
    {
        private string db;
        private SqliteConnection connection;
        private readonly BlockingCollection<object> _queryQueue = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly Task workerTask;


        public QueryDBAccessor(string sqliteDbFile)
        {
            db = sqliteDbFile;
            connection = new SqliteConnection(string.Format("Data Source={0};Mode=ReadWrite;", db));
            connection.Open();
            workerTask = Task.Run(WorkerLoopAsync);
        }

        // Enqueue a query and get a Task<T> for the result
        public Task<T> QueryAsync<T>(string sql, Func<DbDataReader, T> parser)
        {
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
            _cts.Cancel();
            _queryQueue.CompleteAdding();
            workerTask.Wait();

            connection.Close();
            connection.Dispose();
            _cts.Dispose();
            _queryQueue.Dispose();
        }
    }

    public class CommandDBAccessor : IDisposable //fire and forget scenario
    {

        private readonly BlockingCollection<CommandRequest> sqlQueue = new();
        private readonly CancellationTokenSource cts = new();
        private Action<string> WALLogFunc;
        private Action WALFlushFunc;


        private string db;
        private SqliteConnection connection;
        public CommandDBAccessor(string sqliteDbFile, Action<string> logFunc = null, Action flushFunc = null)
        {
            db = sqliteDbFile;
            connection = new SqliteConnection(string.Format("Data Source={0};Mode=ReadWrite;", db));
            connection.Open();
            WALLogFunc = logFunc;
            WALFlushFunc = flushFunc;
            Task.Run(WorkerLoopAsync);

        }

        public void Stop()
        {
            cts.Cancel();
            sqlQueue.CompleteAdding();
        }

        public void Dispose()
        {
            sqlQueue.Dispose();
            cts.Dispose();
            connection.Close();
            connection.Dispose();
        }


        public void EnqueueSql(string sql)
        {
            WALLogFunc(sql);
            var request = new CommandRequest { Sql = sql };
            sqlQueue.Add(request);
        }

        private async Task WorkerLoopAsync()
        {
            try
            {
                foreach (CommandRequest request in sqlQueue.GetConsumingEnumerable(cts.Token))
                {
                    try
                    {
                        using var cmd = connection.CreateCommand();
                        cmd.CommandText = request.Sql;
                        //Console.WriteLine("DBA Executing: {0}", cmd.CommandText);
                        cmd.ExecuteNonQuery();
                        request.CompletionSource.SetResult();
                        WALFlushFunc();
                    }
                    catch (Exception ex)
                    {
                        request.CompletionSource.SetException(ex);
                        Console.WriteLine(ex);

                    }
                }
            }
            catch (OperationCanceledException)
            {
                Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine("CDBA Worker Error: " + ex);
            }
        }

    }
}

