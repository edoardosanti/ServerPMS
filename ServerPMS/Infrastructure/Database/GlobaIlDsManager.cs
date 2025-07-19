// PMS Project V1.0
// LSData - all rights reserved
// IDsManager.cs
//
//

using ServerPMS.Abstractions.Infrastructure.Database;

namespace ServerPMS.Infrastructure.Database
{
    public class GlobalIDsManager : IGlobalIDsManager
    {
         public Dictionary<string, int> OrdersRTD;
         public Dictionary<int, string> OrdersDTR;

         public Dictionary<string, int> UnitsRTD;
         public Dictionary<int, string> UnitsDTR;

        public GlobalIDsManager()
        {
            OrdersRTD = new Dictionary<string, int>();
            OrdersDTR = new Dictionary<int, string>();

            UnitsRTD = new Dictionary<string, int>();
            UnitsDTR = new Dictionary<int, string>();
        }

        public bool RemoveOrderEntry(string runtimeID)
        {
            try
            {

                OrdersDTR.Remove(OrdersRTD[runtimeID]);
                OrdersRTD.Remove(runtimeID);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool RemoveUnitEntry(string runtimeID)
        {
            try
            {

                UnitsDTR.Remove(UnitsRTD[runtimeID]);
                UnitsRTD.Remove(runtimeID);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void AddOrderEntry(string runtimeID, int DBID)
        {
            OrdersDTR.Add(DBID, runtimeID);
            OrdersRTD.Add(runtimeID, DBID);
        }

        public void AddUnitEntry(string runtimeID, int DBID)
        {
            UnitsDTR.Add(DBID, runtimeID);
            UnitsRTD.Add(runtimeID, DBID);
        }

        public int GetOrderDBID(string runtimeID)
        {
            return OrdersRTD[runtimeID];
        }

        public string GetOrderRuntimeID(int DBId)
        {
            return OrdersDTR[DBId];
        }

        public int GetUnitDBID(string runtimeID)
        {
            return UnitsRTD[runtimeID];
        }

        public string GetUnitRuntimeID(int DBId)
        {
            return UnitsDTR[DBId];
        }
    }
}
