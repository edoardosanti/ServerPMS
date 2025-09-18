// PMS Project V1.0
// LSData - all rights reserved
// unitsManager.cs
//
//

using System.Collections;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using ServerPMS.Abstractions.Managers;
using ServerPMS.Abstractions.Infrastructure.Config;
using ServerPMS.Abstractions.Infrastructure.Database;
using ServerPMS.Abstractions.Infrastructure.Concurrency;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using DocumentFormat.OpenXml.ExtendedProperties;

namespace ServerPMS.Managers
{
    public class UnitsManager : IUnitsManager, IDictionary<string, ProductionUnit>
    {

        public event EventHandler<string> NewUnitHandler;

        Dictionary<string, ProductionUnit> units;
        public IEnumerable<string> IDs => units.Keys;
        public Dictionary<string, string> queuesLUT;
        
        private readonly ICommandDBAccessor CmdDBA;
        private readonly IQueryDBAccessor QueryDBA;
        private readonly IQueuesManager QueueMgr;
        private readonly IGlobalConfigManager GlobalConfig;
        private readonly IGlobalIDsManager GlobalIDsManager;
        private readonly IResourceMapper Mapper;


        public UnitsManager(ICommandDBAccessor CDBA, IQueryDBAccessor QDBA,IGlobalIDsManager globalIDManager, IQueuesManager queuesManager, IGlobalConfigManager globalConfig, IResourceMapper mapper)
        {
            units = new Dictionary<string, ProductionUnit>();
            queuesLUT = new Dictionary<string, string>();
            CmdDBA = CDBA;
            QueryDBA = QDBA;
            QueueMgr = queuesManager;
            GlobalConfig = globalConfig;
            GlobalIDsManager = globalIDManager;
            Mapper = mapper;

        }

        //TODO: add update status on DB -> unit.OnStateChanged event (implement) 

        private void _UpdateNotesDB(string unitID, string notes)
        {
            int dbId = GlobalIDsManager.GetUnitDBID(unitID);
            string sql = string.Format("UPDATE prod_units SET notes = {0} WHERE id = {1};", notes, dbId);
            CmdDBA.EnqueueSql(sql); 
        }

        public async Task LoadUnitsAsync()
        {

            Console.WriteLine("**INITIALIZING PRODUCTION units**\n");
            if (GlobalConfig.GlobalRAMConfig.UnitsIDs == null)
                Console.WriteLine("!!! No Production units Found !!!");
            else
            {
                Console.WriteLine("DB_ID\tRUNTIME_ID\t\t\t\tIDENTIFIER\t\tTYPE\t\tNOTES");
                //for each unit conf in unit add unit (info from db record)
                foreach (int DBId in GlobalConfig.GlobalRAMConfig.UnitsIDs)
                {
                    string op = string.Format("SELECT * FROM prod_units WHERE ID = {0}", DBId);
                    Dictionary<string, string> info = await QueryDBA.QueryAsync(op, (DbDataReader dbdr) =>
                    {

                        Dictionary<string, string> row = new();
                        if (dbdr.Read())
                        {
                            for (int i = 0; i < dbdr.FieldCount; i++)
                            {
                                row.Add(dbdr.GetName(i), dbdr.GetValue(i)?.ToString() ?? "NULL");
                            }
                        }
                        return row;
                    });

                    //generate runtimeID
                    string runtimeID = Guid.NewGuid().ToString();

                    //add unit to unit list and lookup table
                    ProductionUnit unit = new ProductionUnit(DBId, info["name"], (UnitType)int.Parse(info["type"]), info["notes"]);
                    unit.UnitNotesUpdateHandler += (sender, notes) => _UpdateNotesDB(runtimeID,notes); //add event for notes update
                    units.Add(runtimeID, unit);
                    OnNewUnit(runtimeID);

                    Mapper.MapUnit(runtimeID, unit); //map resource for concurrency handling
                    GlobalIDsManager.AddUnitEntry(runtimeID, DBId);
                    
                    //create queue and bind
                    BindQueue(runtimeID, QueueMgr.NewQueue(runtimeID));


                    //print infos
                    Console.WriteLine("{0}\t{1}\t{2}\t\t\t{3}\t{4}", DBId, runtimeID, info["name"], (UnitType)int.Parse(info["type"]), info["notes"]);
                }
            }

        }

        public void BindQueue(string unitID, string queueID)
        {
            units[unitID].BindQueue(QueueMgr[queueID]);
        }

        private void OnNewUnit(string unitRuntimeID)
        {
            NewUnitHandler?.Invoke(this, unitRuntimeID);
        }

        public void Start(string unitRuntimeID)
        {
            units[unitRuntimeID].Start();
        }

        public void Stop(string unitRuntimeID)
        {
            units[unitRuntimeID].Stop();
        }

        public string? GetName(string runtimeID)
        {
            try
            {
                return units[runtimeID].Name;
            }
            catch(KeyNotFoundException)
            {
                return null;
            }
            catch
            {
                throw;
            }
        }

        public string? GetNotes(string runtimeID)
        {
            try
            {
                return units[runtimeID].Notes;
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
            catch
            {
                throw;
            }
        }

        #region IDictionary<>
        public ProductionUnit this[string key] { get => ((IDictionary<string, ProductionUnit>)units)[key]; set => ((IDictionary<string, ProductionUnit>)units)[key] = value; }

        public ICollection<string> Keys => ((IDictionary<string, ProductionUnit>)units).Keys;

        public ICollection<ProductionUnit> Values => ((IDictionary<string, ProductionUnit>)units).Values;

        public int Count => ((ICollection<KeyValuePair<string, ProductionUnit>>)units).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<string, ProductionUnit>>)units).IsReadOnly;

        public void Add(string key, ProductionUnit value)
        {
            ((IDictionary<string, ProductionUnit>)units).Add(key, value);
        }

        public void Add(KeyValuePair<string, ProductionUnit> item)
        {
            ((ICollection<KeyValuePair<string, ProductionUnit>>)units).Add(item);
        }

        public void Clear()
        {
            ((ICollection<KeyValuePair<string, ProductionUnit>>)units).Clear();
        }

        public bool Contains(KeyValuePair<string, ProductionUnit> item)
        {
            return ((ICollection<KeyValuePair<string, ProductionUnit>>)units).Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return ((IDictionary<string, ProductionUnit>)units).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, ProductionUnit>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, ProductionUnit>>)units).CopyTo(array, arrayIndex);
        }

        public bool Remove(string key)
        {
            return ((IDictionary<string, ProductionUnit>)units).Remove(key);
        }

        public bool Remove(KeyValuePair<string, ProductionUnit> item)
        {
            return ((ICollection<KeyValuePair<string, ProductionUnit>>)units).Remove(item);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out ProductionUnit value)
        {
            return ((IDictionary<string, ProductionUnit>)units).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator<KeyValuePair<string, ProductionUnit>> IEnumerable<KeyValuePair<string, ProductionUnit>>.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, ProductionUnit>>)units).GetEnumerator();
        }
        #endregion

    } 
}


