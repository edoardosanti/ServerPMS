// PMS Project V1.0
// LSData - all rights reserved
// PMS.cs
//
//
using System;
using Microsoft.Data.Sqlite;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Data.Common;

namespace ServerPMS
{
    public class PMSCore
    {

        bool RAMOnlyMode=false;

        List<ProductionOrder> ProductionOrdersBuffer;

        private delegate void CDBADelegate(string sql);
        private delegate Task<string> QDBADataDelegate(string sql);
        private delegate Task<Dictionary<string,string>> QDBARowDelegate(string sql);

        public CommandDBAccessor CmdDBA;
        CDBADelegate CDBAOperation;
        public QueryDBAccessor QueryDBA;
        QDBADataDelegate QDBADataOperation;
        QDBARowDelegate QDBARowOperation;

        public PMSCore()
        {
   
            //initialize WAL
            WALLogger logger = new WALLogger(GlobalConfigManager.GlobalRAMConfig.WAL.WALFilePath);




            //DB OPERATING ENVIROMENT INITIALIZATION
            //open service connection
            using var conn = new SqliteConnection(string.Format("Data Source={0};", GlobalConfigManager.GlobalRAMConfig.Database.FilePath));
            conn.Open();

            //enable WAL mode
            using var cmd = new SqliteCommand("PRAGMA journal_mode=WAL;", conn);  //enable Write Ahead Log (WAl) mode
            cmd.ExecuteNonQuery();

            //starts DBAs
            CmdDBA = new CommandDBAccessor(GlobalConfigManager.GlobalRAMConfig.Database.FilePath, logger.Log,logger.Flush);
            QueryDBA = new QueryDBAccessor(GlobalConfigManager.GlobalRAMConfig.Database.FilePath);

            //define delegates to encapsulate logging and DB operations -- probabilemente da cambiare
            CDBAOperation = CmdDBA.EnqueueSql;
            QDBADataOperation = async (string sql) => {return await QueryDBA.QueryAsync(sql, (DbDataReader dbdr) => {
                return dbdr.Read() ? dbdr.GetString(0) : "UNKNOWN";
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


            //ORDER LIST LOADING
            ProductionOrdersBuffer = new List<ProductionOrder>();

            
            //PRODUCTION ENVIROMENT INITIALIZATION
            ProdcutionEnviroment PE = new ProdcutionEnviroment();

            if (GlobalConfigManager.GlobalRAMConfig.ProdEnv.units == null)
                Console.WriteLine("!!! No Production Units Found !!!");
            else
            {
                foreach(ProdUnitConf conf in GlobalConfigManager.GlobalRAMConfig.ProdEnv.units)
                {
                    string op = string.Format("SELECT * FROM prod_units WHERE ID = {0}", conf.DBId);
                    Dictionary<string,string> info = QDBARowOperation(op).GetAwaiter().GetResult();
                    int localId = PE.AddUnit((UnitType)int.Parse(info["type"]),info["notes"]);
                    //Console.WriteLine(PE.Units.Find(x => x.ID == localId).ToInfo());
                }
            }


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

