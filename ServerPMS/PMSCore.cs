// PMS Project V1.0
// LSData - all rights reserved
// PMSCore.cs
//
//

using DocumentFormat.OpenXml.Drawing.Diagrams;
using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace ServerPMS
{
    public class PMSCore
    {

        bool RAMOnlyMode=false;

        List<ProductionOrder> ProductionOrdersBuffer;

        public delegate void CDBADelegate(string sql);
        public delegate Task CDBAAwaitableDelegate(string sql);
        public delegate void CDBATransactionOp(Guid id);
        public delegate Task CDBAAwaitableTransactionOp(Guid id);
        public delegate Task<string> QDBADataDelegate(string sql);
        public delegate Task<Dictionary<string,string>> QDBARowDelegate(string sql);

        public CommandDBAccessor CmdDBA;
        public CDBADelegate CDBAOperation;
        public CDBAAwaitableDelegate CDBAAwaitableOperation;
        public CDBATransactionOp CDBACommitOperation;
        public CDBATransactionOp CDBARollbackOperation;
        public CDBAAwaitableTransactionOp CDBAAwaitableCommitOperation;
        public CDBAAwaitableTransactionOp CDBAAwaitableRollbackOperation;

        public QueryDBAccessor QueryDBA;
        public QDBADataDelegate QDBADataOperation;
        public QDBARowDelegate QDBARowOperation;


        public PMSCore()
        {

            //DB OPERATING ENVIROMENT INITIALIZATION
            #region
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
            #endregion



            //REPLAY WAL IF NEEDED
            #region
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



            //ORDER LIST LOADING
            #region

            //DEBUG
            string filename = "/Users/edoardosanti/Downloads/TEST_IRS_2.xlsx";


            OrderManager manager = new OrderManager(CmdDBA, QueryDBA);

            manager.LoadFromExcelFile(filename,
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
                    ));


            ProductionOrdersBuffer = new List<ProductionOrder>();


            #endregion


            //PRODUCTION ENVIROMENT INITIALIZATION
            #region
            //initialize prodenv
            ProductionEnviroment PE = new ProductionEnviroment();

            if (GlobalConfigManager.GlobalRAMConfig.ProdEnv.units == null)
                Console.WriteLine("!!! No Production Units Found !!!");
            else
            {
                //for each unit conf in unit add unit (info from db record)
                foreach(ProdUnitConf conf in GlobalConfigManager.GlobalRAMConfig.ProdEnv.units)
                {
                    string op = string.Format("SELECT * FROM prod_units WHERE ID = {0}", conf.DBId);
                    Dictionary<string,string> info = QDBARowOperation(op).GetAwaiter().GetResult();
                    int localId = PE.AddUnit((UnitType)int.Parse(info["type"]),info["notes"]);
                    //Console.WriteLine(PE.Units.Find(x => x.ID == localId).ToInfo());
                }
            }
            #endregion

        }

        public bool ImportOrdersFromExcelFile(string filename, ExcelOrderParserParams parserParams=null)
        {
            ExcelOrderParser excelParser;
            if (parserParams != null)
                excelParser = new ExcelOrderParser(filename, parserParams);
            else
                excelParser = new ExcelOrderParser(filename,
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

            List<ProductionOrder> import = excelParser.ParseOrders();
            excelParser.Dispose();

            if (import.Count > 0)
            {
                ProductionOrdersBuffer.Concat(import); //TODO: check ìf order already in system
                return true;
            }
            else
                return false;
        }

        public string StrDumpBuffer()
        {
            string s = string.Empty;
            foreach(ProductionOrder order in ProductionOrdersBuffer)
            {
                s += order.ToInfo() + "\n";
            }
            return s;
        }

        public void AddOrder(ProductionOrder order)
        {
            ProductionOrdersBuffer.Add(order);
        }

        public void AddOrder(string partCode, string partDescription, int qty, string customerOrderRef, int defaultProdUnit, string moldID, string moldLocation, string moldNotes, string customerName, string deliveryFacility, string deliveryDate)
        {
            ProductionOrdersBuffer.Add(new ProductionOrder(partCode, partDescription, qty, customerOrderRef, defaultProdUnit, moldID, moldLocation, moldNotes, customerName, deliveryFacility, deliveryDate)); ;
        }
    }
}

