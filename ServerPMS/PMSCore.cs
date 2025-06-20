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
    public struct PMSCoreSettings
    {
        public string WAL_PATH;
        public string SQLITE_DB_FILE;
    }

    public class PMSCore
    {

        bool RAMOnlyMode=false;

        List<ProductionOrder> ProductionOrdersBuffer;

        private delegate void CDBADelegate(string sql);
        private delegate Task<string> QDBADelegate(string sql);

        public CommandDBAccessor CmdDBA;
        CDBADelegate CDBAOperation;
        public QueryDBAccessor QueryDBA;
        QDBADelegate QDBAOperation;

        public PMSCore(PMSCoreSettings settings)
        {
   
            //initialize WAL
            WALLogger logger = new WALLogger(settings.WAL_PATH);

            //initialize db env
            using var conn = new SqliteConnection(string.Format("Data Source={0};", settings.SQLITE_DB_FILE));
            conn.Open();
            using var cmd = new SqliteCommand("PRAGMA journal_mode=WAL;", conn);  //enable Write Ahead Log (WAl) mode
            cmd.ExecuteNonQuery();
            CmdDBA = new CommandDBAccessor(settings.SQLITE_DB_FILE,logger.Log,logger.Flush);
            QueryDBA = new QueryDBAccessor(settings.SQLITE_DB_FILE);

            //define delegates to encapsulate logging and DB operations -- probabilemente da cambiare
            CDBAOperation = CmdDBA.EnqueueSql;
            QDBAOperation = async (string sql) => {return await QueryDBA.QueryAsync(sql, (DbDataReader dbdr) => { return dbdr.Read() ? dbdr.GetString(0) : "UNKNOWN"; }); };

            ProductionOrdersBuffer = new List<ProductionOrder>();

            //initializing production enviroment
            ProdcutionEnviroment PE = new ProdcutionEnviroment();
            PE.AddUnit(UnitType.MoldingMachine, "Krauss Maffei");
            PE.AddUnit(UnitType.MoldingMachine, "Negri Bossi");
            PE.AddUnit(UnitType.MoldingMachine, "Battenfeld");
            PE.AddUnit(UnitType.CNCLathe, "Haas");

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

