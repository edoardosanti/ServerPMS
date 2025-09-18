// PMS Project V1.0
// LSData - all rights reserved
// OrdersManager.cs
//
//
using System.Data.Common;
using Microsoft.Extensions.Logging;
using ServerPMS.Core;
using ServerPMS.Abstractions.Managers;
using ServerPMS.Infrastructure.External;
using ServerPMS.Abstractions.Infrastructure.Concurrency;
using ServerPMS.Abstractions.Infrastructure.Database;

namespace ServerPMS.Managers
{
    public class OrdersManager : IOrdersManager
    {

        //DBAs
        private readonly ICommandDBAccessor CmdDBA;
        private readonly IQueryDBAccessor QueryDBA;
        private readonly IGlobalIDsManager GlobalIDsManager;
        private readonly ILogger<OrdersManager> Logger;
        private readonly IResourceMapper Mapper;

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

        public ProductionOrder this[string runtimeID] => buffer[indexLookupTable[runtimeID]];

        public OrdersManager(ICommandDBAccessor CDBA, IQueryDBAccessor QDBA,IGlobalIDsManager globalIDsManager,ILogger<OrdersManager> logger, IResourceMapper mapper)
        {
            CmdDBA = CDBA;
            QueryDBA = QDBA;
            GlobalIDsManager = globalIDsManager;
            Logger = logger;
            Mapper = mapper;
            indexLookupTable = new Dictionary<string, int>();

            buffer = new OrdersBuffer(GlobalIDsManager,Mapper);
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

            Logger.LogInformation("Orders Manager started.");
        }


        #region events callers methods
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
        #endregion

        //set order state in DB 
        public void UpdateOrderState(string runtimeID, OrderState newState)
        {
            int dbId, status;
            dbId = GlobalIDsManager.GetOrderDBID(runtimeID);
            status = (int)newState;
            string sql = string.Format("UPDATE prod_orders SET status = {0} WHERE id = {1};", status, dbId);
            CmdDBA.EnqueueSql(sql);
            buffer[indexLookupTable[runtimeID]].UpdateOrderStatus(newState);

            Logger.LogInformation("Order state changed");
        }

        //remove order from orders buffer, order table and queues (using predicate)
        public bool RemoveNotEOFOrder(Predicate<ProductionOrder> predicate)
        {
            ProductionOrder target = buffer.Find(predicate);
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
            return RemoveNotEOFOrder(x => x.RuntimeID == runtimeID);
        }

        //load into Buffer orders from DB
        public async Task LoadOrdersFromDBAsync()
        {

            //leggere ordini da DB e aggiungerli al buffer
            List<ProductionOrder> fromDB = await QueryDBA.QueryAsync("SELECT * FROM prod_orders",(DbDataReader dbdr) =>
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
            );
      
            buffer.SmartAdd(fromDB);

            Logger.LogInformation("Loaded orders from DB");

           

        }

        //write order to DB
        private void WriteOrderToDB(ProductionOrder order)
        {
            string values = string.Format("'{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}'",
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
            string sql = string.Format("INSERT INTO prod_orders(part_code, part_desc, qty, customer_ord_ref, default_prod_unit, mold_id, mold_location, mold_notes, customer_name, delivery_facility, delivery_date, status) VALUES({0});", values);
            CmdDBA.EnqueueSql(sql);
            Logger.LogInformation("Order sent to CDBA for writing.");
        }

        //write array of orders to DB
        private void WriteOrdersToDB(IEnumerable<ProductionOrder> orders)
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
        public async Task LoadFromExcelFileAsync(string filename, ExcelOrderParserParams parserParams)
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
                    await LoadOrdersFromDBAsync();
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

        public void Import(IEnumerable<ProductionOrder> orders)
        {
            buffer.SmartAdd(orders);
            WriteOrdersToDB(orders);
        }
    }
}

