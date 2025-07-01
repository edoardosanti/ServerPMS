// PMS Project V1.0
// LSData - all rights reserved
// PMSCore.cs
//
//
using Microsoft.Data.Sqlite;
using System.Data.Common;


namespace ServerPMS
{
    public class PMSCore
    {
        public OrdersManager OrdersMgr;
        public Dictionary<string, ProductionUnit> Units;
        public Dictionary<string, ReorderableQueue<string>> Queues;

        public delegate void CDBADelegate(string sql);
        public delegate Task CDBAAwaitableDelegate(string sql);
        public delegate void CDBATransactionDelegate(string sql, Guid id);
        public delegate void CDBATransactionCRDelegate(Guid id);
        public delegate Task CDBAAwaitableTransactionOpDelegate(Guid id);
        
        public delegate Task<string> QDBADataDelegate(string sql);
        public delegate Task<Dictionary<string,string>> QDBARowDelegate(string sql);
        public delegate Task<List<string>> QDBAMultiRowDelegate(string sql);


        public CommandDBAccessor CmdDBA;
        public CDBADelegate CDBAOperation;
        public CDBAAwaitableDelegate CDBAAwaitableOperation;
        public CDBATransactionCRDelegate CDBACommitOperation;
        public CDBATransactionCRDelegate CDBARollbackOperation;
        public CDBAAwaitableTransactionOpDelegate CDBAAwaitableCommitOperation;
        public CDBAAwaitableTransactionOpDelegate CDBAAwaitableRollbackOperation;
        public CDBATransactionDelegate CDBATransactionOperation;
       
        public QueryDBAccessor QueryDBA;
        public QDBADataDelegate QDBADataOperation;
        public QDBAMultiRowDelegate QDBAMultiRowOperation;
        public QDBARowDelegate QDBARowOperation;


        public PMSCore()
        {

            #region DB OPERATING ENVIROMENT INITIALIZATION
            //open service connection
            using var conn = new SqliteConnection(string.Format("Data Source={0};", GlobalConfigManager.GlobalRAMConfig.Database.FilePath));
            conn.Open();

            //enable WAL mode (DB and application-layer)
            using var cmd = new SqliteCommand("PRAGMA journal_mode=WAL;", conn);  //enable Write Ahead Log (WAl) mode
            cmd.ExecuteNonQuery();
            WALLogger.WALFilePath = GlobalConfigManager.GlobalRAMConfig.WAL.WALFilePath;
            WALLogger.Start();

            //starts DBAs
            CmdDBA = new CommandDBAccessor(GlobalConfigManager.GlobalRAMConfig.Database.FilePath, WALLogger.Log,WALLogger.Flush);
            QueryDBA = new QueryDBAccessor(GlobalConfigManager.GlobalRAMConfig.Database.FilePath);

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

            #region REPLAY WAL IF NEEDED
            //for each SQL command in WAL
            foreach (string op in WALLogger.Replay())
            {
                //send command to CDBA and wait for execution
                if(op.Substring(0,6)== "#CDBA#")
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
            #endregion

            #region MANAGERS AND IEM ENVIROMENT INITIALIZAION

            OrdersMgr = new OrdersManager(CmdDBA,QueryDBA);
            OrdersMgr.LoadOrdersFromDB();
            

            #endregion

            #region PRODUCTION ENVIROMENT INITIALIZATION

            //initialize units and queues
            Units = new Dictionary<string, ProductionUnit>();
            QueuesManager QM = new QueuesManager(CmdDBA, QueryDBA);

            //adding units to env
            Console.WriteLine("**INITIALIZING PRODUCTION UNITS**\n");
            if (GlobalConfigManager.GlobalRAMConfig.UnitsIDs == null)
                Console.WriteLine("!!! No Production Units Found !!!");
            else
            {
                Console.WriteLine("DB_ID\tRUNTIME_ID\t\t\t\tIDENTIFIER\t\tTYPE\t\tNOTES");

                //for each unit conf in unit add unit (info from db record)
                foreach (int DBId in GlobalConfigManager.GlobalRAMConfig.UnitsIDs)
                {
                    //get units info from DB
                    string op = string.Format("SELECT * FROM prod_units WHERE ID = {0}", DBId);
                    Dictionary<string, string> info = QDBARowOperation(op).GetAwaiter().GetResult();

                    //generate runtimeID
                    string runtimeID = Guid.NewGuid().ToString();

                    //add unit to unit list and lookup table
                    Units.Add(runtimeID, new ProductionUnit(DBId, (UnitType)int.Parse(info["type"]), info["notes"]));
                    GlobalIDsManager.AddUnitEntry(runtimeID, DBId);

                    //add queue to QueueManager and load queue from DB
                    QM.NewQueue(runtimeID);
                    QM.LoadQueue(runtimeID);

                    Console.WriteLine("{0}\t{1}\t{2}\t\t\t{3}\t{4}", DBId, runtimeID, info["name"], ((UnitType)int.Parse(info["type"])).ToString(), info["notes"]);
                }
            }

            #endregion

        }

        

        public string StrDumpBuffer()
        {
            string s = string.Empty;
            foreach(ProductionOrder order in OrdersMgr.OrdersBuffer)
            { 
                s += order.ToInfo() + "\n";
            }
            return s;
        }

        #region UNITS OPERATIONS

        public void StopUnit(string runtimeUnitID)
        {
            Units[runtimeUnitID].Stop();
        }

        #endregion

        #region DATABASE OPERATIONS

        //load into Buffer orders from DB

        #endregion

        #region FILE OPERATIONS

        public void ImportOrdersFromExcelFile(string filename, ExcelOrderParserParams parserParams = null)
        {
            if (parserParams != null)
                OrdersMgr.LoadFromExcelFile(filename, parserParams);
            else
                OrdersMgr.LoadFromExcelFile(filename,
                new ExcelOrderParserParams(
                    "CODE",
                    "DESCRIPTION",
                    "QUANTITY",
                    "ORDINE",
                    "MACCHINA",
                    "STAMPO",
                    "P_STAMPO",
                    "NOTE_STAMPO",
                    "CLIENTE", "" +
                    "MAGAZZINO_CONSEGNA",
                    "DATA_CONSEGNA"
                    )
                );

        }

        #endregion
    }
}

