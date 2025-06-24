// PMS Project V1.0
// LSData - all rights reserved
// OrderManager.cs
//
//
using System.Data.Common;
using DocumentFormat.OpenXml.EMMA;

namespace ServerPMS
{
    public class OrderManager
    {
        public OrderBuffer Buffer;
        public event OrdersUpdatedHandler OnOrdersUpdate;

        public delegate void OrdersUpdatedHandler();
        delegate void CDBADelegate(string sql);
        delegate Task<string> QDBADataDelegate(string sql);
        delegate Task<string> QDBARowDelegate(string sql);
        delegate Task<List<string>> QDBAMultiRowDelegate(string sql);

        CDBADelegate CDBAOperation;
        QDBADataDelegate QDBADataOperation;
        QDBAMultiRowDelegate QDBAMultiRowOperation;
        QDBARowDelegate QDBARowOperation;
        
        CommandDBAccessor CDBA;
        QueryDBAccessor QDBA;

        public OrderManager(CommandDBAccessor CDBA, QueryDBAccessor QDBA)
        {
            Buffer = new OrderBuffer(); 

            this.CDBA = CDBA;
            this.QDBA = QDBA;

            CDBAOperation = (string sql) => { CDBA.EnqueueSql(sql); };
            QDBAMultiRowOperation = async (string sql) =>
            {
                return await QDBA.QueryAsync(sql, (DbDataReader dbdr) =>
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
            QDBARowOperation = async (string sql) =>
            {
                return await QDBA.QueryAsync(sql, (DbDataReader dbdr) =>
                {
                    string order = string.Empty;
                    if (dbdr.Read())
                    {
                        for (int i = 0; i < dbdr.FieldCount; i++)
                        {
                            order += (dbdr.GetValue(i)?.ToString() ?? "NULL") + "$";
                        }
                        return order;
                    }
                    return "NULL";
                });
            };
            QDBADataOperation = async (string sql) => {
                return await QDBA.QueryAsync(sql, (DbDataReader dbdr) => {
                    return dbdr.Read() ? dbdr.GetString(0) : "NULL";
                });
            };

            LoadFromDB();

        }

        private void LoadFromDB()
        {
            List<ProductionOrder> fromDB = new List<ProductionOrder>();
            //leggere ordini da DB e aggiungerli al bufffer
            List<string> rows = QDBAMultiRowOperation("SELECT * FROM prod_orders").GetAwaiter().GetResult();
            foreach(string row in rows)
            {
                fromDB.Add(ProductionOrder.FromDump(row));
                
            }
            Buffer.SmartAdd(fromDB);

            foreach(ProductionOrder order in Buffer)
            {
                Console.WriteLine(order.ToShortInfo());
            }
        }

        private void WriteToDB(ProductionOrder order)
        {
            string values = string.Format("'{0}','{1}','{2}',{3},'{4}',{5},'{6}','{7}','{8}','{9}','{10}','{11}',{12}",
                order.RuntimeID,
                order.PartCode,
                order.PartDescription,
                order.Qty,
                order.CustomerOrderRef,
                order.DefaultProductionUnit,
                order.MoldID,
                order.MoldLocation,
                order.MoldNotes,
                order.CustomerName,
                order.DeliveryFacility,
                order.DeliveryDate,
                (int)order.OrderStatus);
            string sql = string.Format("INSERT INTO prod_orders(ID_pms, part_code, part_desc, qty, customer_ord_ref, default_prod_unit, mold_id, mold_location, mold_notes, customer_name, delivery_facility, delivery_date, order_status) VALUES({0});",values);
            CDBAOperation(sql);
        }

        private void WriteToDB(ProductionOrder[] orders)
        {
            if (orders != null)
            {
                foreach (ProductionOrder order in orders)
                {
                    WriteToDB(order);
                }
            }
        }

        public void LoadFromExcelFile(string filename,ExcelOrderParserParams parserParams)
        {
            if (File.Exists(filename))
            {
                List<ProductionOrder> parsed = ParseFromExcel(filename, parserParams);
                if (parsed != null)
                {
                    foreach (ProductionOrder order in parsed)
                    {
                        WriteToDB(order);
                    }
                    LoadFromDB();
                }
                else
                    throw new NullReferenceException("No orders found");
            }
            else
                throw new FileNotFoundException("File not found", filename);

            
        }

        private List<ProductionOrder> ParseFromExcel(string filename, ExcelOrderParserParams parserParams)
        {
            //create a parser and parse orders, then dispose parser
            using (ExcelOrderParser parser = new ExcelOrderParser(filename, parserParams)) {
                 return parser.ParseOrders();
            }
            
        }
    }
}

