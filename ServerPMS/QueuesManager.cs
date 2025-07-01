// PMS Project V1.0
// LSData - all rights reserved
// QueuesManager.cs
//
//
using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace ServerPMS
{

    public class QueuesManager
    {

        CommandDBAccessor CmdDBA;
        QueryDBAccessor QueryDBA;

        Dictionary<string, ReorderableQueue<string>> Queues;

        public QueuesManager(CommandDBAccessor CBDA, QueryDBAccessor QDBA)
        {
            Queues = new Dictionary<string, ReorderableQueue<string>>();
            CmdDBA = CBDA;
            QueryDBA = QDBA;

        }

        public void NewQueue(string runtimeID)
        {
            Queues.Add(runtimeID, new ReorderableQueue<string>());
        }

        public void NewQueue(object sender, NewUnitEventArgs args)
        {
            Queues.Add(args.RuntimeID, new ReorderableQueue<string>());
        }

        public Task LoadQueueAsync(string runtimeID)
        {
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

        public void AddToQueue(string orderRuntimeID, string queueRuntimeID)
        {
            List<string> transactionSQLs = new List<string>();

            int orderDBID = GlobalIDsManager.GetOrderDBID(orderRuntimeID);
            int queueBDID = GlobalIDsManager.GetUnitDBID(queueRuntimeID);

            transactionSQLs.Add(string.Format("INSERT INTO units_queues(unit_id, order_id) VALUES({0},{1});", queueBDID, orderDBID));
            transactionSQLs.Add(string.Format("UPDATE prod_orders SET status = 1 WHERE ID = {0};" ,orderDBID));

            CmdDBA.NewTransactionAndCommit(transactionSQLs.ToArray());

            Queues[queueRuntimeID].Enqueue(orderRuntimeID);

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

        //TODO add all queue operations logic (add to queue, remove, move up/down)

    }
}

