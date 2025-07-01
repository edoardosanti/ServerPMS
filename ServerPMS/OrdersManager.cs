// PMS Project V1.0
// LSData - all rights reserved
// OrdersManager.cs
//
//
using System.Data.Common;


namespace ServerPMS
{
    public class OrdersManager
    {

        //DBAs
        CommandDBAccessor CmdDBA;
        QueryDBAccessor QueryDBA;

        //main order buffer
        OrdersBuffer buffer;
        public OrdersBuffer OrdersBuffer => buffer;

        //events
        public event EventHandler<ProductionOrder> OrderAddedHandler;
        public event EventHandler<ProductionOrder> OrderStateImported;
        public event EventHandler<ProductionOrder> OrderStateInQueue;
        public event EventHandler<ProductionOrder> OrderStateInProduction;
        public event EventHandler<ProductionOrder> OrderStateCompleted;
        public event EventHandler<ProductionOrder> OrderRemovedHandler;

        Dictionary<string, int> indexLookupTable;

        public OrdersManager(CommandDBAccessor CDBA, QueryDBAccessor QDBA)
        {
            CmdDBA = CDBA;
            QueryDBA = QDBA;

            indexLookupTable = new Dictionary<string, int>();

            buffer = new OrdersBuffer();
            buffer.ItemAddedHandler += (object sender, ProductionOrder order)=> {

                OnOrderAdded(order);

                //reference the method to call when the order state changes
                order.StateChangedHandler += StateEventDispatcher;

                //add entry on lookup table
                indexLookupTable.Add(order.RuntimeID, buffer.IndexOf(order));
            };
            buffer.ItemRemovedHandler += (object sender, ProductionOrder order) =>
            {
                indexLookupTable.Remove(order.RuntimeID);
                OnOrderRemoved(order);
            };
            
        }


        //events callers methods
        private void OnOrderAdded(ProductionOrder order)
        {
            OrderAddedHandler?.Invoke(this, order);
        }

        private void OnStateImported(ProductionOrder order)
        {
            OrderStateImported?.Invoke(this, order);
        }

        private void OnStateInQueue(ProductionOrder order)
        {
            OrderStateInQueue?.Invoke(this, order);
        }

        private void OnStateInProduction(ProductionOrder order)
        {
            OrderStateInProduction?.Invoke(this, order);
        }

        private void OnStateCompleted(ProductionOrder order)
        {
            OrderStateCompleted?.Invoke(this, order);
        }

        private void OnOrderRemoved(ProductionOrder order)
        {
            OrderRemovedHandler?.Invoke(this, order);
        }

        private void StateEventDispatcher(object sender, OrderState state)
        {
            //debug
            Console.WriteLine("Order: {0} | State: {1}", (sender as ProductionOrder).RuntimeID, state.ToString());

            switch (state)
            {
                case OrderState.Imported:
                    OnStateImported(sender as ProductionOrder);
                    break;

                case OrderState.InQueue:
                    OnStateInQueue(sender as ProductionOrder);
                    break;

                case OrderState.InProduction:
                    OnStateInProduction(sender as ProductionOrder);
                    break;

                case OrderState.Completed:
                    OnStateCompleted(sender as ProductionOrder);
                    break;
            }
        }

        //set order state in DB 
        public void UpdateOrderState(string runtimeID, OrderState newState)
        {
            int dbId, status;
            dbId = GlobalIDsManager.GetOrderDBID(runtimeID);
            status = (int)newState;
            string sql = string.Format("UPDATE prod_orders SET status = {0} WHERE id = {1};", status, dbId);
            CmdDBA.EnqueueSql(sql);
            buffer[indexLookupTable[runtimeID]].ChangeState(newState);
        }

        //remove order from orders buffer, order table and queues (using predicate)
        public bool RemoveNotEOFOrder(Predicate<ProductionOrder> predicate)
        {
            ProductionOrder target = buffer.Find(predicate);
            GlobalIDsManager.RemoveOrderEntry(target.RuntimeID);

            string[] sqls =
            {
                string.Format("DELETE FROM prod_orders WHERE ID={0};",GlobalIDsManager.GetOrderDBID(target.RuntimeID)),
                string.Format("DELETE FROM units_queues WHERE order_id={0};",GlobalIDsManager.GetOrderDBID(target.RuntimeID))
            };

            CmdDBA.NewTransactionAndCommit(sqls);
            return buffer.Remove(predicate);
        }

        //remove order from orders buffer, order table and queues (using runtimeID)
        public bool RemoveNotEOFOrder(string runtimeID)
        {
            GlobalIDsManager.RemoveOrderEntry(runtimeID);
            return RemoveNotEOFOrder(x => x.RuntimeID == runtimeID);
        }

        //load into Buffer orders from DB
        public void LoadOrdersFromDB()
        {

            //leggere ordini da DB e aggiungerli al buffer
            List<ProductionOrder> fromDB = QueryDBA.QueryAsync("SELECT * FROM prod_orders",(DbDataReader dbdr) =>
            {
                List<ProductionOrder> _fromDB = new();

                while (dbdr.Read())
                {
                    string order = string.Empty;
                    for (int i = 0; i < dbdr.FieldCount; i++)
                    {
                        order += (dbdr.GetValue(i)?.ToString() ?? "NULL") + "$";
                    }
                    try
                    {
                        _fromDB.Add(ProductionOrder.FromDump(order));
                    }
                    catch { }
                }
                return _fromDB;
            }
            ).GetAwaiter().GetResult();
      
            buffer.SmartAdd(fromDB);

        }

        //write order to DB
        private void WriteOrderToDB(ProductionOrder order)
        {
            string values = string.Format("'{0}','{1}','{2}',{3},'{4}',{5},'{6}','{7}','{8}','{9}','{10}','{11}'",
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
            string sql = string.Format("INSERT INTO prod_orders(part_code, part_desc, qty, customer_ord_ref, default_prod_unit, mold_id, mold_location, mold_notes, customer_name, delivery_facility, delivery_date, order_status) VALUES({0});", values);
            CmdDBA.EnqueueSql(sql);
        }

        //write array of orders to DB
        private void WriteOrdersToDB(ProductionOrder[] orders)
        {
            if (orders != null)
            {
                foreach (ProductionOrder order in orders)
                {
                    WriteOrderToDB(order);
                }
            }
        }

        //load orders from excel file to main buffer
        public void LoadFromExcelFile(string filename, ExcelOrderParserParams parserParams)
        {
            if (File.Exists(filename))
            {
                List<ProductionOrder> parsed = ParseFromExcel(filename, parserParams);
                if (parsed != null)
                {
                    foreach (ProductionOrder order in parsed)
                    {
                        WriteOrderToDB(order);
                    }
                    LoadOrdersFromDB();
                }
                else
                    throw new NullReferenceException("No orders found");
            }
            else
                throw new FileNotFoundException("File not found", filename);


        }

        //parse orders from excel file
        private List<ProductionOrder> ParseFromExcel(string filename, ExcelOrderParserParams parserParams)
        {
            //create a parser and parse orders, then dispose parser
            using (ExcelOrderParser parser = new ExcelOrderParser(filename, parserParams))
            {
                return parser.ParseOrders();
            }

        }

    }
}

