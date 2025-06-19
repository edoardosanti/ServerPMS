// PMS Project V1.0
// LSData - all rights reserved
// PMS.cs
//
//
using System;

namespace ServerPMS
{
    public class PMSCore
    {

        List<ProductionOrder> ProductionOrdersBuffer;
        
        public PMSCore()
        {

            
            string filename = string.Empty;

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
                ProductionOrdersBuffer = import;
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

