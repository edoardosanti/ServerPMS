// PMS Project V1.0
// LSData - all rights reserved
// IntegratedEventManager.cs
//
//
using ServerPMS.Infrastructure.Database;
using ServerPMS.Abstractions.Managers;
using ServerPMS.Abstractions.Infrastructure.Database;

namespace ServerPMS.Managers
{

    public class IntegratedEventsManager : IIntegratedEventsManager
    {
        private readonly IOrdersManager OrdersMgr;
        private readonly IUnitsManager UnitsMgr;
        private readonly IQueuesManager QueuesMgr;

        private readonly ICommandDBAccessor CmdDBA;
        private readonly IQueryDBAccessor QueryDBA;
        private readonly IGlobalIDsManager GlobalIDsManager;


        public IntegratedEventsManager(IOrdersManager ordersMgr, IUnitsManager unitsMgr, IQueuesManager queuesMgr, ICommandDBAccessor CDBA, IQueryDBAccessor QDBA, IGlobalIDsManager globalIDsManager)
        {
            OrdersMgr = ordersMgr;
            UnitsMgr = unitsMgr;
            QueuesMgr = queuesMgr;

            CmdDBA = CDBA;
            QueryDBA = QDBA;
            GlobalIDsManager = globalIDsManager;

        }


        #region Queues Operations

        public void Enqueue(string queueRuntimeID, string orderRuntimeID)
        {
            //retrieve DB IDs

            int[] DBIds = { GlobalIDsManager.GetUnitDBID(queueRuntimeID), GlobalIDsManager.GetOrderDBID(orderRuntimeID) };

            if (OrdersMgr[orderRuntimeID].OrderStatus != OrderState.Imported)
                throw new InvalidOperationException("Orders must be in \"Imported\" state before enqueueing. ");

            string[] sqls =
            {
                string.Format("UPDATE prod_orders SET status = {0} WHERE ID = {1};",(int)OrderState.InQueue,DBIds[1]),
                string.Format(
                    "INSERT INTO units_queues (unit_id, order_id, position) " +
                    "VALUES ({0}, {1}, COALESCE((SELECT MAX(position) FROM units_queues WHERE unit_id = {0}), -1) + 1);",
                    DBIds[0],DBIds[1]),
            };

            //create and commit transaction
            CmdDBA.NewTransactionAndCommit(sqls);

            //update ram state
            OrdersMgr.OrdersBuffer[orderRuntimeID].UpdateOrderStatus(OrderState.InQueue);

            //update ram queue
            QueuesMgr[queueRuntimeID].Enqueue(orderRuntimeID);
        }

        public void MoveUpInQueue(string queueRuntimeID, string orderRuntimeID, int steps = 1)
        {
            QueuesMgr[queueRuntimeID].MoveUp(orderRuntimeID, steps);
        }

        public void MoveDownInQueue(string queueRuntimeID, string orderRuntimeID, int steps = 1)
        {
            QueuesMgr[queueRuntimeID].MoveDown(orderRuntimeID, steps);
        }

        public void MoveInQueue(string queueRuntimeID, string orderRuntimeID, int steps = 1)
        {
            QueuesMgr[queueRuntimeID].Move(orderRuntimeID, steps);
        }

        public string DequeueAndComplete(string queueRuntimeID)
        {
            string orderRuntimeID = QueuesMgr[queueRuntimeID].Dequeue();

            //get DBIds
            int orderDBID = GlobalIDsManager.GetOrderDBID(orderRuntimeID);
            int queueBDID = GlobalIDsManager.GetUnitDBID(queueRuntimeID);

            string[] transactionSQLs =
            {
            string.Format("DELETE FROM units_queues WHERE unit_id = {0} AND order_id = {1}", queueBDID, orderDBID),
            string.Format("UPDATE prod_orders SET status = 3 WHERE ID = {0};", orderDBID)
        };

            //commit transaction
            CmdDBA.NewTransactionAndCommit(transactionSQLs);

            OrdersMgr[orderRuntimeID].UpdateOrderStatus(OrderState.Completed);

            return orderRuntimeID;
        }

        public int PositionOf(string queueRuntimeID, string orderRuntimeID)
        {
            return QueuesMgr[queueRuntimeID].PositionOf(orderRuntimeID);
        }

        public string FindInQueue(string orderRuntimeID)
        {
            foreach (string id in QueuesMgr.IDs)
            {
                if (QueuesMgr[id].Contains(orderRuntimeID))
                    return id;
            }
            return string.Empty;
        }

        public void RemoveFromQueueNotEOF(string queueRuntimeID, string orderRuntimeID)
        {

            //get DBIds
            int orderDBID = GlobalIDsManager.GetOrderDBID(orderRuntimeID);
            int queueBDID = GlobalIDsManager.GetUnitDBID(queueRuntimeID);

            string[] transactionSQLs =
            {
            string.Format("DELETE FROM units_queues WHERE unit_id = {0} AND order_id = {1}", queueBDID, orderDBID),
            string.Format("UPDATE prod_orders SET status = 0 WHERE ID = {0};", orderDBID)
            };

            //commit transaction
            CmdDBA.NewTransactionAndCommit(transactionSQLs);
            QueuesMgr[queueRuntimeID].Remove(orderRuntimeID);

        }

        #endregion
    }

}