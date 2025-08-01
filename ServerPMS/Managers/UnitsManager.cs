﻿// PMS Project V1.0
// LSData - all rights reserved
// UnitsManager.cs
//
//

using System.Collections;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using ServerPMS.Infrastructure.Database;
using ServerPMS.Abstractions.Managers;
using ServerPMS.Abstractions.Infrastructure.Config;
using ServerPMS.Abstractions.Infrastructure.Database;

namespace ServerPMS.Managers
{
    
    public class UnitsManager : IUnitsManager, IDictionary<string, ProductionUnit>
    {

        public event EventHandler<string> NewUnitHandler;

        Dictionary<string, ProductionUnit> units;
        public Dictionary<string, ProductionUnit> Units => units;
        
        private readonly ICommandDBAccessor CmdDBA;
        private readonly IQueryDBAccessor QueryDBA;
        private readonly IQueuesManager QueueMgr;
        private readonly IGlobalConfigManager GlobalConfig;
        private readonly IGlobalIDsManager GlobalIDsManager;


        public UnitsManager(
            ICommandDBAccessor CDBA, IQueryDBAccessor QDBA,IGlobalIDsManager globalIDManager,
            IQueuesManager queuesManager,
            IGlobalConfigManager globalConfig
            )
        {
            units = new Dictionary<string, ProductionUnit>();
            CmdDBA = CDBA;
            QueryDBA = QDBA;
            QueueMgr = queuesManager;
            GlobalConfig = globalConfig;
            GlobalIDsManager = globalIDManager;

        }

        public void LoadUnits()
        {

            Console.WriteLine("**INITIALIZING PRODUCTION Units**\n");
            if (GlobalConfig.GlobalRAMConfig.UnitsIDs == null)
                Console.WriteLine("!!! No Production Units Found !!!");
            else
            {
                Console.WriteLine("DB_ID\tRUNTIME_ID\t\t\t\tIDENTIFIER\t\tTYPE\t\tNOTES");
                //for each unit conf in unit add unit (info from db record)
                foreach (int DBId in GlobalConfig.GlobalRAMConfig.UnitsIDs)
                {
                    string op = string.Format("SELECT * FROM prod_Units WHERE ID = {0}", DBId);
                    Dictionary<string, string> info = QueryDBA.QueryAsync(op, (DbDataReader dbdr) =>
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
                    }).GetAwaiter().GetResult();

                    //generate runtimeID
                    string runtimeID = Guid.NewGuid().ToString();

                    //add unit to unit list and lookup table
                    units.Add(runtimeID, new ProductionUnit(DBId, (UnitType)int.Parse(info["type"]), info["notes"]));
                    OnNewUnit(runtimeID);
                    GlobalIDsManager.AddUnitEntry(runtimeID, DBId);
                    QueueMgr.NewQueue(runtimeID);


                    //print infos
                    Console.WriteLine("{0}\t{1}\t{2}\t\t\t{3}\t{4}", DBId, runtimeID, info["name"], (UnitType)int.Parse(info["type"]), info["notes"]);
                }
            }

        }

        void OnNewUnit(string unitRuntimeID)
        {
            NewUnitHandler?.Invoke(this, unitRuntimeID);
        }

        public void Start(string unitRuntimeID)
        {
            units[unitRuntimeID].Start();
        }

        public void Stop(string unitRuntimeID)
        {

        }

        #region IDictionary<>
        public ProductionUnit this[string key] { get => ((IDictionary<string, ProductionUnit>)Units)[key]; set => ((IDictionary<string, ProductionUnit>)Units)[key] = value; }

        public ICollection<string> Keys => ((IDictionary<string, ProductionUnit>)Units).Keys;

        public ICollection<ProductionUnit> Values => ((IDictionary<string, ProductionUnit>)Units).Values;

        public int Count => ((ICollection<KeyValuePair<string, ProductionUnit>>)Units).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<string, ProductionUnit>>)Units).IsReadOnly;

        public void Add(string key, ProductionUnit value)
        {
            ((IDictionary<string, ProductionUnit>)Units).Add(key, value);
        }

        public void Add(KeyValuePair<string, ProductionUnit> item)
        {
            ((ICollection<KeyValuePair<string, ProductionUnit>>)Units).Add(item);
        }

        public void Clear()
        {
            ((ICollection<KeyValuePair<string, ProductionUnit>>)Units).Clear();
        }

        public bool Contains(KeyValuePair<string, ProductionUnit> item)
        {
            return ((ICollection<KeyValuePair<string, ProductionUnit>>)Units).Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return ((IDictionary<string, ProductionUnit>)Units).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, ProductionUnit>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, ProductionUnit>>)Units).CopyTo(array, arrayIndex);
        }

        public bool Remove(string key)
        {
            return ((IDictionary<string, ProductionUnit>)Units).Remove(key);
        }

        public bool Remove(KeyValuePair<string, ProductionUnit> item)
        {
            return ((ICollection<KeyValuePair<string, ProductionUnit>>)Units).Remove(item);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out ProductionUnit value)
        {
            return ((IDictionary<string, ProductionUnit>)Units).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator<KeyValuePair<string, ProductionUnit>> IEnumerable<KeyValuePair<string, ProductionUnit>>.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, ProductionUnit>>)Units).GetEnumerator();
        }
        #endregion

    } 
}


