// PMS Project V2.0
// LSData - all rights reserved
// QueuesManager.cs
//
//

using System.Data.Common;
using Microsoft.Extensions.Logging;
using ServerPMS.Abstractions.Managers;
using ServerPMS.Infrastructure.Generic;
using ServerPMS.Abstractions.Infrastructure.Database;
using ServerPMS.Abstractions.Infrastructure.Concurrency;
using ServerPMS.Infrastructure.ClientCommunication;
using System.Diagnostics.CodeAnalysis;
using System.Collections;

namespace ServerPMS.Managers
{

    public class QueuesManager : IQueuesManager, IEnumerable<UnitQueue>, IDictionary<string, UnitQueue>
    {
        #region Events Handlers
        public event EventHandler<string> ItemEnqueuedHandler;


        #endregion

        #region Properties

        private readonly ICommandDBAccessor CmdDBA;
        private readonly IQueryDBAccessor QueryDBA;
        private readonly ILogger<QueuesManager> Logger;
        private readonly IGlobalIDsManager GlobalIDsManager;
        private readonly IResourceMapper Mapper;

        Dictionary<string, UnitQueue> Queues;

        public UnitQueue this[string runtimeID] => Queues[runtimeID];
        public IEnumerable<string> IDs => Queues.Keys;

        public ICollection<string> Keys => ((IDictionary<string, UnitQueue>)Queues).Keys;

        public ICollection<UnitQueue> Values => ((IDictionary<string, UnitQueue>)Queues).Values;

        public int Count => ((ICollection<KeyValuePair<string, UnitQueue>>)Queues).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<string, UnitQueue>>)Queues).IsReadOnly;

        UnitQueue IDictionary<string, UnitQueue>.this[string key] { get => ((IDictionary<string, UnitQueue>)Queues)[key]; set => ((IDictionary<string, UnitQueue>)Queues)[key] = value; }

        #endregion

        #region Constructors
        public QueuesManager(ICommandDBAccessor CBDA,IQueryDBAccessor QDBA,ILogger<QueuesManager> logger,IGlobalIDsManager globalIDsManager,IResourceMapper mapper )
        {
            Queues = new Dictionary<string, UnitQueue>();
            CmdDBA = CBDA;
            QueryDBA = QDBA;
            Logger = logger;
            Mapper = mapper;
            GlobalIDsManager = globalIDsManager;

            Logger.LogInformation("Queues Manager started.");
        }

        #endregion

        #region Manager Operations


        public string NewQueue(string? bindTo=null)
        {
            string runtimeID = Guid.NewGuid().ToString();
            UnitQueue queue;
            if (bindTo != null)
                queue = new UnitQueue(runtimeID, bindTo);
            else
                queue = new UnitQueue(runtimeID);

            Queues.Add(runtimeID, queue);
            Queues[runtimeID].ItemMovedHandler += (object sender, string orderRuntimeID) =>
            {
                _UpdatePositionDB(orderRuntimeID, (sender as ReorderableQueue<string>).PositionOf(orderRuntimeID));
            };

            Mapper.MapQueue(runtimeID, queue); //map resource for concurrency handling
            Logger.LogInformation("Queue added {0}.", runtimeID);
            return runtimeID;
            
        }

        public Task LoadQueueAsync(string runtimeID)
        {
            UnitQueue queue = Queues[runtimeID];
            int dbUnitID;

            if (queue.IsBinded) //if unit is binded to queue
            {
                dbUnitID = GlobalIDsManager.GetUnitDBID(queue.BindedUnitID);

                string sql = string.Format("SELECT * FROM units_queues WHERE unit_id={0};", dbUnitID);

                Logger.LogInformation("Started loading op queue {0}.", runtimeID);

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
            else
                throw new InvalidOperationException("The queue is not binded to any production unit");
        }

        public void LoadQueue(string runtimeID)
        {
            try
            {
                LoadQueueAsync(runtimeID).Wait();
            }
            catch
            {
                throw;
            }
        }

        public void LoadAll()
        {
            foreach (string key in Queues.Keys)
            {
                try {
                    LoadQueueAsync(key).Wait();
                }
                catch(Exception ex)
                {
                    Logger.LogError(ex.Message);
                }
            }
        }

        public async Task LoadAllAsync()
        {
            foreach (string key in Queues.Keys)
            {
                try
                {
                    await LoadQueueAsync(key);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex.Message);
                }
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

        public Report GetReport()
        {
            return null;
        }

        public void Add(string key, UnitQueue value)
        {
            ((IDictionary<string, UnitQueue>)Queues).Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return ((IDictionary<string, UnitQueue>)Queues).ContainsKey(key);
        }

        public bool Remove(string key)
        {
            return ((IDictionary<string, UnitQueue>)Queues).Remove(key);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out UnitQueue value)
        {
            return ((IDictionary<string, UnitQueue>)Queues).TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<string, UnitQueue> item)
        {
            ((ICollection<KeyValuePair<string, UnitQueue>>)Queues).Add(item);
        }

        public void Clear()
        {
            ((ICollection<KeyValuePair<string, UnitQueue>>)Queues).Clear();
        }

        public bool Contains(KeyValuePair<string, UnitQueue> item)
        {
            return ((ICollection<KeyValuePair<string, UnitQueue>>)Queues).Contains(item);
        }

        public void CopyTo(KeyValuePair<string, UnitQueue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, UnitQueue>>)Queues).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, UnitQueue> item)
        {
            return ((ICollection<KeyValuePair<string, UnitQueue>>)Queues).Remove(item);
        }

        public IEnumerator<KeyValuePair<string, UnitQueue>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, UnitQueue>>)Queues).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Queues).GetEnumerator();
        }

        IEnumerator<UnitQueue> IEnumerable<UnitQueue>.GetEnumerator()
        {
            return Values.GetEnumerator();
        }


        //TODO add all queue operations logic (add to queue, remove, move up/down)

        #endregion
    }

}

