// PMS Project V1.0
// LSData - all rights reserved
// InegratedEventManager.cs
//
//
using System;
using DocumentFormat.OpenXml.Office2010.Excel;
using System.Net.NetworkInformation;

namespace ServerPMS
{
    public class IntegratedEventManager
    {
        OrdersManager OrdersMgr;
        UnitsManager UnitsMgr;
        QueuesManager QueuesMgr;

        CommandDBAccessor CmdDBA;
        QueryDBAccessor QueryDBA;


        public IntegratedEventManager(OrdersManager ordersMgr, UnitsManager unitsMgr, QueuesManager queuesMgr, CommandDBAccessor CDBA, QueryDBAccessor QDBA)
        {
            OrdersMgr = ordersMgr;
            UnitsMgr = unitsMgr;
            QueuesMgr = queuesMgr;

            CmdDBA = CDBA;
            QueryDBA = QDBA;

        }

        public void AddToQueue(string queueRuntimeID, string orderRuntimeID)
        { 
            //retrieve DB IDs
            int[] DBIds = { GlobalIDsManager.GetUnitDBID(queueRuntimeID), GlobalIDsManager.GetOrderDBID(orderRuntimeID) };

            string[] sqls =
            {
                string.Format("UPDATE prod_orders SET status = {0} WHERE ID = {1};",(int)OrderState.InQueue,DBIds[1]),
                string.Format(
                    "INSERT INTO unit_queues (unit_id, order_id, position) " +
                    "VALUES ({0}, {1}, COALESCE((SELECT MAX(position) FROM unit_queues WHERE unit_id = {0}), -1) + 1);",
                    DBIds[0],DBIds[1]),
            };

            //create and commit transaction
            CmdDBA.NewTransactionAndCommit(sqls);

            //update ram state
            OrdersMgr.OrdersBuffer[orderRuntimeID].ChangeState(OrderState.InQueue);

            //update ram queue
            QueuesMgr[queueRuntimeID].Enqueue(orderRuntimeID);
        }
    }
}

