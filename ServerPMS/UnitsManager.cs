// PMS Project V1.0
// LSData - all rights reserved
// UnitsManager.cs
//
//
using System;
using System.Collections;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using DocumentFormat.OpenXml.Bibliography;
using static ServerPMS.PMSCore;

namespace ServerPMS
{
    public class NewUnitEventArgs : EventArgs
    {
        public string RuntimeID { get; set; }
    }

    public class UnitsManager : IDictionary<string, ProductionUnit>
    {

        public event EventHandler NewUnitHandler;

        Dictionary<string, ProductionUnit> units;
        CommandDBAccessor CmdDBA;
        QueryDBAccessor QueryDBA;

        public UnitsManager(CommandDBAccessor CDBA, QueryDBAccessor QDBA)
        {
            units = new Dictionary<string, ProductionUnit>();
            CmdDBA = CDBA;
            QueryDBA = QDBA;
        }

        public void LoadUnits()
        {

            Console.WriteLine("**INITIALIZING PRODUCTION UNITS**\n");
            if (GlobalConfigManager.GlobalRAMConfig.UnitsIDs == null)
                Console.WriteLine("!!! No Production Units Found !!!");
            else
            {
                Console.WriteLine("DB_ID\tRUNTIME_ID\t\t\t\tIDENTIFIER\t\tTYPE\t\tNOTES");
                //for each unit conf in unit add unit (info from db record)
                foreach (int DBId in GlobalConfigManager.GlobalRAMConfig.UnitsIDs)
                {
                    string op = string.Format("SELECT * FROM prod_units WHERE ID = {0}", DBId);
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
                    OnNewUnit(new NewUnitEventArgs { RuntimeID = runtimeID });
                    GlobalIDsManager.AddUnitEntry(runtimeID, DBId);

                }
            }

        }

        void OnNewUnit(EventArgs args)
        {
            if (NewUnitHandler != null)
            {
                NewUnitHandler(this, args);
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


