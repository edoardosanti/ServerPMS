// PMS Project V1.0
// LSData - all rights reserved
// PMSCore.cs
//
//

using Microsoft.Data.Sqlite;
using System.Data.Common;
using Microsoft.Extensions.Logging;

using ServerPMS.Abstractions.Infrastructure.Database;
using ServerPMS.Abstractions.Infrastructure.Logging;
using ServerPMS.Abstractions.Infrastructure.Config;
using ServerPMS.Abstractions.Managers;
using ServerPMS.Abstractions.Core;

namespace ServerPMS.Core
{
    internal class AppCore: IAppCore
    {
        //dipendecies
        private readonly ICommandDBAccessor CmdDBA;
        private readonly IQueryDBAccessor QueryDBA;

        private readonly IOrdersManager OrdersMgr;
        private readonly IQueuesManager QueuesMgr;
        private readonly IUnitsManager UnitsMgr;
        private readonly IIntegratedEventsManager IEM;

        private readonly ILogger<AppCore> Logger;
        private readonly IWALLogger WAL;

        private readonly IGlobalConfigManager GlobalConfig;

        #region DBA DELEGATES
        public delegate void CDBADelegate(string sql);
        public delegate Task CDBAAwaitableDelegate(string sql);
        public delegate void CDBATransactionDelegate(string sql, Guid id);
        public delegate void CDBATransactionCRDelegate(Guid id);
        public delegate Task CDBAAwaitableTransactionOpDelegate(Guid id);
        
        public delegate Task<string> QDBADataDelegate(string sql);
        public delegate Task<Dictionary<string,string>> QDBARowDelegate(string sql);
        public delegate Task<List<string>> QDBAMultiRowDelegate(string sql);

        CDBADelegate CDBAOperation;
        CDBAAwaitableDelegate CDBAAwaitableOperation;
        CDBATransactionCRDelegate CDBACommitOperation;
        CDBATransactionCRDelegate CDBARollbackOperation;
        CDBAAwaitableTransactionOpDelegate CDBAAwaitableCommitOperation;
        CDBAAwaitableTransactionOpDelegate CDBAAwaitableRollbackOperation;
        CDBATransactionDelegate CDBATransactionOperation;
       
        QDBADataDelegate QDBADataOperation;
        QDBAMultiRowDelegate QDBAMultiRowOperation;
        QDBARowDelegate QDBARowOperation;
        #endregion

        public AppCore(
            ICommandDBAccessor commandDBA ,IQueryDBAccessor queryDBA,
            IOrdersManager ordersManager, IQueuesManager queuesManager, IUnitsManager unitsManager,
            IIntegratedEventsManager integratedEventsManager,
            ILogger<AppCore> logger, IWALLogger WALLogger,
            IGlobalConfigManager globalConfigManager

            )
        {

            #region Dipendencies

            CmdDBA = commandDBA;
            QueryDBA = queryDBA;

            OrdersMgr = ordersManager;
            QueuesMgr = queuesManager;
            UnitsMgr = unitsManager;
            IEM = integratedEventsManager;

            GlobalConfig = globalConfigManager;

            WAL = WALLogger;
            Logger = logger;
            #endregion

            #region Initialize DB operations delegates
 
            //define delegates to encapsulate logging and DB operations -- probabilemente da cambiare
            CDBAOperation = (string sql) => { CmdDBA.EnqueueSql(sql); };
            CDBAAwaitableOperation = CmdDBA.EnqueueSql;
            CDBATransactionOperation = (string sql, Guid id) => { CmdDBA.EnqueueSql(sql, id); };
            CDBACommitOperation = (Guid id) => { CmdDBA.EnqueueTransactionCommit(id); } ;
            CDBARollbackOperation = (Guid id) => { CmdDBA.EnqueueTransactionRollback(id); };
            CDBAAwaitableCommitOperation = CmdDBA.EnqueueTransactionCommit;
            CDBAAwaitableRollbackOperation = CmdDBA.EnqueueTransactionRollback;

            QDBADataOperation = async (string sql) => {return await QueryDBA.QueryAsync(sql, (DbDataReader dbdr) => {
                return dbdr.Read() ? dbdr.GetString(0) : "NULL";
            }); };
            QDBARowOperation = async (string sql) => { return await QueryDBA.QueryAsync(sql, (DbDataReader dbdr) =>
            {
                Dictionary<string,string> row = new Dictionary<string, string>();
                if (dbdr.Read())
                {
                    for (int i = 0; i < dbdr.FieldCount; i++)
                    {
                        row.Add(dbdr.GetName(i),dbdr.GetValue(i)?.ToString() ?? "NULL");
                    }
                }
                return row;
            }); };
            QDBAMultiRowOperation = async (string sql) =>
            {
                return await QueryDBA.QueryAsync(sql, (DbDataReader dbdr) =>
                {

                    List<string> rows = new();

                    while (dbdr.Read())
                    {
                        string order = string.Empty;
                        for (int i = 0; i < dbdr.FieldCount; i++)
                        {
                            order += (dbdr.GetValue(i)?.ToString() ?? "NULL") + "$";
                        }
                        rows.Add(order);
                    }

                    return rows;
                });
            };
            #endregion

            #region Initialize managers

            //initalize order manager and load orders from DB
            Logger.LogInformation("Starting managers (Orders, Units, Queues)");

            OrdersMgr.LoadOrdersFromDB();

            Console.WriteLine("**IMPORTED ORDERS**");
            foreach(ProductionOrder order in OrdersMgr.OrdersBuffer)
            {
                Console.WriteLine(order.ToShortInfo());
            }

            //initalize units manager
            UnitsMgr.LoadUnits();

       
            //add queue to QueueManager and load queue from DB
            foreach (string runtimeID in UnitsMgr.Units.Keys)
            {
                Console.WriteLine("Queue:\t{0}\t|\tCount:\t{1}\t", runtimeID, QueuesMgr[runtimeID].Count);
            }



            #endregion

        }

        public void InitializeWALEnviroment()
        {
            //open service connection
            Logger.LogInformation("Opening SQLite service connection");

            using var conn = new SqliteConnection(string.Format("Data Source={0};", GlobalConfig.GlobalRAMConfig.Database.FilePath));
            conn.Open();

            //enable WAL mode (DB and application-layer)
            Logger.LogInformation("Enabling SQLite WAL mode");

            using var cmd = new SqliteCommand("PRAGMA journal_mode=WAL;", conn);  //enable Write Ahead Log (WAl) mode
            cmd.ExecuteNonQuery();

            Logger.LogInformation("Enabling Application-Layer WAL mode");

            WAL.WALFilePath = GlobalConfig.GlobalRAMConfig.WAL.WALFilePath;
        }

        public Task WALReplay()
        {
            Logger.LogInformation("Replaying WAL if needed.");

            //for each SQL command in WAL
            foreach (string op in WAL.Replay())
            {
                //send command to CDBA and wait for execution
                if (op.Substring(0, 6) == "#CDBA#")
                {
                    string[] command = op.Replace("#CDBA#", "").Split(":");
                    switch (command.ElementAt(0))
                    {
                        case "C":
                            CDBAAwaitableCommitOperation(Guid.Parse(command[1])).Wait();
                            break;
                        case "R":
                            CDBAAwaitableRollbackOperation(Guid.Parse(command[1])).Wait();
                            break;
                    }
                }
                else
                    CDBAAwaitableOperation(op).Wait();
            }

            return Task.CompletedTask;
        }

        public string ToInfo()
        {
            string info = string.Empty;

            info += "Avaiable Orders:\n";
            foreach(ProductionOrder order in OrdersMgr.OrdersBuffer)
            {
                info += order.ToShortInfo() + "\n";
            }

            info += "\n\nAvaiable Units:\n";
            foreach (ProductionUnit unit in UnitsMgr.Units.Values)
            {
                info += unit.ToInfo();
            }

            info += "\n\n Queues:\n";
            foreach (string id in UnitsMgr.Units.Keys)
            {
                info += QueuesMgr[id].ToInfo();
            }
            return info;

        }

       

        #region FILE OPERATIONS

        //public void ImportOrdersFromExcelFile(string filename, ExcelOrderParserParams parserParams = null)
        //{
        //    if (parserParams != null)
        //        OrdersMgr.LoadFromExcelFile(filename, parserParams);
        //    else
        //        OrdersMgr.LoadFromExcelFile(filename,
        //        new ExcelOrderParserParams(
        //            "CODE",
        //            "DESCRIPTION",
        //            "QUANTITY",
        //            "ORDINE",
        //            "MACCHINA",
        //            "STAMPO",
        //            "P_STAMPO",
        //            "NOTE_STAMPO",
        //            "CLIENTE", "" +
        //            "MAGAZZINO_CONSEGNA",
        //            "DATA_CONSEGNA"
        //            )
        //        );

        //}

        #endregion
    }
}

