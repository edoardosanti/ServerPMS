using System.Collections.Concurrent;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using ServerPMS.Abstractions.Infrastructure.Config;
using ServerPMS.Abstractions.Infrastructure.Database;
using ServerPMS.Abstractions.Infrastructure.Logging;
using Microsoft.Extensions.Hosting;
using System;

namespace ServerPMS.Infrastructure.Database
{
    public class CommandDBAccessor : BackgroundService, ICommandDBAccessor, IDisposable  //fire and forget scenario
    {

        private readonly BlockingCollection<CDBARequest> sqlQueue = new();
        private string db;
        private SqliteConnection connection;
        public ConcurrentDictionary<Guid, SqliteTransaction> TransactionsTable;

        private readonly ILogger<CommandDBAccessor> Logger;
        private readonly IGlobalConfigManager GlobalConfig;
        private readonly IWALLogger WAL;

        private CancellationTokenSource cts;

        public bool IsRunning => _isRunning;

        private volatile bool _isRunning;

        private Task mainTask;


        public CommandDBAccessor(IGlobalConfigManager configManager, ILogger<CommandDBAccessor> logger, IWALLogger WALLogger)
        {
            Logger = logger;
            WAL = WALLogger;
            GlobalConfig = configManager;

            Logger.LogInformation("CDBA - Starting.");

            db = GlobalConfig.GlobalRAMConfig.Database.FilePath;

            connection = new SqliteConnection(string.Format("Data Source={0};Mode=ReadWrite;", db));
            connection.Open();
            TransactionsTable = new ConcurrentDictionary<Guid, SqliteTransaction>();

            _isRunning = false;
        }

        public Task EnqueueSql(string sql, Guid CDBATransactionIdentifier)
        {
            Logger.LogInformation("CDBA - Enqued SQL: {0} TX: {1}", sql, CDBATransactionIdentifier.ToString());

            WAL.Log(sql);
            var request = new CDBARequest { Sql = sql, Type = CDBARequestType.SQLCommand, TransactionID = CDBATransactionIdentifier };
            sqlQueue.Add(request);
            return request.CompletionSource.Task;
        }

        public Task EnqueueSql(string sql)
        {
            Logger.LogInformation("CDBA - Enqued SQL: {0}", sql);

            WAL.Log(sql);
            var request = new CDBARequest { Sql = sql, Type = CDBARequestType.SQLCommand, TransactionID = null };
            sqlQueue.Add(request);
            return request.CompletionSource.Task;
        }

        public Task EnqueueTransactionCommit(Guid CDBATransactionIdentifier)
        {
            Logger.LogInformation("CDBA - Enqued Commit TX: {0}", CDBATransactionIdentifier.ToString());

            WAL.Log(string.Format("#CDBA#C:{0}", CDBATransactionIdentifier.ToString()));
            var request = new CDBARequest { Sql = "", Type = CDBARequestType.TransactionCommit, TransactionID = CDBATransactionIdentifier };
            sqlQueue.Add(request);
            return request.CompletionSource.Task;
        }

        public Task EnqueueTransactionRollback(Guid CDBATransactionIdentifier)
        {
            Logger.LogInformation("CDBA - Enqued Rollback TX: {0}", CDBATransactionIdentifier.ToString());

            WAL.Log(string.Format("#CDBA#R:{0}", CDBATransactionIdentifier.ToString()));
            var request = new CDBARequest { Sql = "", Type = CDBARequestType.TransactionRollback, TransactionID = CDBATransactionIdentifier };
            sqlQueue.Add(request);
            return request.CompletionSource.Task;
        }

        public Task NewTransactionAndCommit(string[] sqls)
        {

            string tmp = "\n";
            foreach (string s in sqls)
            {
                tmp += s + "\n";
            }
            Logger.LogInformation("CDBA - Enqued SQLs in new internal TX: {0}", tmp);


            if (sqls != null)
            {
                Guid guid = Guid.NewGuid();
                TransactionsTable.TryAdd(guid, connection.BeginTransaction());
                foreach (string sql in sqls)
                {
                    EnqueueSql(sql, guid);
                }
                return EnqueueTransactionCommit(guid);
            }
            return null;

        }

        public Guid NewTransaction()
        {
            Guid guid = Guid.NewGuid();
            Logger.LogInformation("CDBA - New transaction: {0}", guid.ToString());

            TransactionsTable.TryAdd(guid, connection.BeginTransaction());
            return guid;
        }

        private async Task WorkerLoopAsync()
        {
            Logger.LogInformation($"CDBA - Worker loop starting. (Thread: {Thread.CurrentThread.ManagedThreadId} )");

            try
            {
                foreach (CDBARequest request in sqlQueue.GetConsumingEnumerable(cts.Token))
                {
                    try
                    {
                        switch (request.Type)
                        {

                            case CDBARequestType.SQLCommand:
                                //create a sqlite command
                                using (var cmd = connection.CreateCommand())
                                {
                                    cmd.CommandText = request.Sql;


                                    //if a transactionID is specified assing the command to the respective SQLite transaction using lookup table 
                                    if (request.TransactionID.HasValue)
                                    {
                                        SqliteTransaction tx = TransactionsTable[request.TransactionID.Value];
                                        cmd.Transaction = tx;

                                    }
                                    //execute command
                                    Logger.LogInformation("CDBA - Executing SQL: {0}", cmd.CommandText);
                                    await cmd.ExecuteNonQueryAsync();

                                }

                                request.CompletionSource.SetResult();
                                WAL.Flush();
                                break;

                            case CDBARequestType.TransactionCommit:
                                Logger.LogInformation("CDBA - Executing Commit TX: {0}", request.TransactionID);
                                SqliteTransaction commitTX = TransactionsTable[request.TransactionID.Value];
                                await commitTX.CommitAsync();
                                TransactionsTable.TryRemove(request.TransactionID.Value, out _);
                                request.CompletionSource.SetResult();
                                WAL.Flush();
                                break;

                            case CDBARequestType.TransactionRollback:
                                Logger.LogInformation("CDBA - Executing Rollback TX: {0}", request.TransactionID);
                                SqliteTransaction rollbackTX = TransactionsTable[request.TransactionID.Value];
                                await rollbackTX.RollbackAsync();
                                TransactionsTable.TryRemove(request.TransactionID.Value,out _);
                                request.CompletionSource.SetResult();
                                WAL.Flush();
                                break;
                        }


                    }
                    catch (Exception ex)
                    {
                        request.CompletionSource.SetException(ex);
                        if (request.TransactionID.HasValue && TransactionsTable.TryGetValue(request.TransactionID.Value, out var tx))
                        {
                            await tx.RollbackAsync();
                            TransactionsTable.TryRemove(request.TransactionID.Value, out _);
                            WAL.Flush();
                        }
                        Console.WriteLine(ex);

                    }
                }
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation("CDBA - Shutting Down.");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("CDBA Worker Error: " + ex);
                throw;
            }
        }

        public override void Dispose()
        {
            sqlQueue.Dispose();
            connection.Dispose();
            base.Dispose();
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("CDBA - Shutting Down");

            sqlQueue.CompleteAdding();

            if (mainTask != null)
            {
                await mainTask;
            }

            foreach (var pair in TransactionsTable)
            {
                try { pair.Value.Rollback(); } catch { }
                pair.Value.Dispose();
            }
            TransactionsTable.Clear();

            connection.Close();

            await base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            try
            {
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
    }
}

