// PMS Project V2.0
// LSData - all rights reserved
// QueuesManager.cs
//
//

using System.Data.Common;
using Microsoft.Extensions.Logging;

namespace ServerPMS
{

    public class QueuesManager
    {
        #region Events Handlers
        public event EventHandler<string> ItemEnqueuedHandler;


        #endregion

        #region Properties

        CommandDBAccessor CmdDBA;
        QueryDBAccessor QueryDBA;
        Dictionary<string, ReorderableQueue<string>> Queues;

        public ReorderableQueue<string> this[string runtimeID] => Queues[runtimeID];
        public IEnumerable<string> IDs => Queues.Keys;

        #endregion

        #region Constructors
        public QueuesManager(CommandDBAccessor CBDA, QueryDBAccessor QDBA)
        {
            Queues = new Dictionary<string, ReorderableQueue<string>>();
            CmdDBA = CBDA;
            QueryDBA = QDBA;

            Loggers.Queues.LogInformation("Queues Manager started.");
        }

        #endregion

        #region Manager Operations

        public void NewQueue(string runtimeID)
        {
            Queues.Add(runtimeID, new ReorderableQueue<string>());
            Queues[runtimeID].ItemMovedHandler += (object sender, string orderRuntimeID) =>
            {
                _UpdatePositionDB(orderRuntimeID, (sender as ReorderableQueue<string>).PositionOf(orderRuntimeID));
            };
            Loggers.Queues.LogInformation("Queue added {0}.",runtimeID);

        }

        public Task LoadQueueAsync(string runtimeID)
        {

            //TODO CHECK introduces duplicates 
            int dbUnitID = GlobalIDsManager.GetUnitDBID(runtimeID);

            string sql = string.Format("SELECT * FROM units_queues WHERE unit_id={0};", dbUnitID);

            Loggers.Queues.LogInformation("Started loading op queue {0}.", runtimeID);

            return QueryDBA.QueryAsync(sql, (DbDataReader dbdr) =>
            {
                //use SortedDict to store the entries sorted by position
                SortedDictionary<int, string> posTbl = new SortedDictionary<int, string>();

                while (dbdr.Read())
                {
                    //add every line to the sorted dict
                    posTbl.Add(dbdr.GetInt32(3), GlobalIDsManager.GetOrderRuntimeID(dbdr.GetInt32(2)));
                }

                //enqueue all the elements in the sorted dict 
                Queues[runtimeID].SmartEnqueue(posTbl.Values);

                return 0;
            });

           
        }
        public void LoadQueue(string runtimeID)
        {
            LoadQueueAsync(runtimeID).Wait();
        }

        public void LoadAll()
        {
            foreach (string key in Queues.Keys)
            {
                LoadQueueAsync(key);
            }
        }

        private void _OnItemEnqueued(string queueRuntimeID)
        {
            ItemEnqueuedHandler?.Invoke(this, queueRuntimeID);
        }

        private Task _UpdatePositionDB(string orderRuntimeID, int newPosition)
        {
            if (newPosition < 0)
                throw new InvalidOperationException("New position must be non-negative.");

            string sql = string.Format("UPDATE unit_queues SET position = {0} WHERE ID = {1};",newPosition, GlobalIDsManager.GetOrderDBID(orderRuntimeID));
            return CmdDBA.EnqueueSql(sql);
        }


        //TODO add all queue operations logic (add to queue, remove, move up/down)

        #endregion
    }

}

