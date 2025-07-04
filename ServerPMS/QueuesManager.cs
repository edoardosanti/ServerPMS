// PMS Project V1.0
// LSData - all rights reserved
// QueuesManager.cs
//
//
using System.ComponentModel;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace ServerPMS
{

    public class QueuesManager
    {

        CommandDBAccessor CmdDBA;
        QueryDBAccessor QueryDBA;

        public event EventHandler<string> ItemEnqueuedHandler;

        public Dictionary<string, ReorderableQueue<string>> Queues;

        public QueuesManager(CommandDBAccessor CBDA, QueryDBAccessor QDBA)
        {
            Queues = new Dictionary<string, ReorderableQueue<string>>();
            CmdDBA = CBDA;
            QueryDBA = QDBA;

        }

        public ReorderableQueue<string> this[string runtimeID] => Queues[runtimeID];

        public void NewQueue(string runtimeID)
        {
            Queues.Add(runtimeID, new ReorderableQueue<string>());
        }

        public void NewQueue(object sender, string runtimeID)
        {
            Queues.Add(runtimeID, new ReorderableQueue<string>());
        }

        public Task LoadQueueAsync(string runtimeID)
        {

            //TODO TOFIX  introduces duplicates 
            int dbUnitID = GlobalIDsManager.GetUnitDBID(runtimeID);

            string sql = string.Format("SELECT * FROM units_queues WHERE unit_id={0};", dbUnitID);

            return QueryDBA.QueryAsync(sql, (DbDataReader dbdr) =>
            {
                while(dbdr.Read())
                {
                    
                    Queues[runtimeID].Enqueue(GlobalIDsManager.GetOrderRuntimeID(dbdr.GetInt32(2)));

                }
                return 0;
            });

        }

        public void LoadQueue(string runtimeID)
        {
            LoadQueueAsync(runtimeID).Wait();
        }
        
        public void LoadAll()
        {
            foreach(string key in Queues.Keys)
            {
                LoadQueueAsync(key);
            }
        }

        public void RemoveFromQueue(string orderRuntimeID, string queueRuntimeID)
        {
            List<string> transactionSQLs = new List<string>();

            int orderDBID = GlobalIDsManager.GetOrderDBID(orderRuntimeID);
            int queueBDID = GlobalIDsManager.GetUnitDBID(queueRuntimeID);

            transactionSQLs.Add(string.Format("DELETE FROM units_queues WHERE unit_id = {0} AND order_id = {1}", queueBDID, orderDBID));
            transactionSQLs.Add(string.Format("UPDATE prod_orders SET status = 0 WHERE ID = {0};", orderDBID));

            CmdDBA.NewTransactionAndCommit(transactionSQLs.ToArray());

            Queues[queueRuntimeID].Dequeue(orderRuntimeID);

        }

        private void OnItemEnqueued(string queueRuntimeID)
        {
            ItemEnqueuedHandler?.Invoke(this, queueRuntimeID); 
        }

        //TODO add all queue operations logic (add to queue, remove, move up/down)

    }
}

