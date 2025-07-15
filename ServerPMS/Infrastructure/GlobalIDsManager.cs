// PMS Project V1.0
// LSData - all rights reserved
// IDsManager.cs
//
//
namespace ServerPMS
{
    public static class GlobalIDsManager
    {
         public static Dictionary<string, int> OrdersRTD;
         public static Dictionary<int, string> OrdersDTR;

         public static Dictionary<string, int> UnitsRTD;
         public static Dictionary<int, string> UnitsDTR;

        static GlobalIDsManager()
        {
            OrdersRTD = new Dictionary<string, int>();
            OrdersDTR = new Dictionary<int, string>();

            UnitsRTD = new Dictionary<string, int>();
            UnitsDTR = new Dictionary<int, string>();
        }

        public static bool RemoveOrderEntry(string runtimeID)
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

        public static bool RemoveUnitEntry(string runtimeID)
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

        public static void AddOrderEntry(string runtimeID, int DBID)
        {
            OrdersDTR.Add(DBID, runtimeID);
            OrdersRTD.Add(runtimeID, DBID);
        }

        public static void AddUnitEntry(string runtimeID, int DBID)
        {
            UnitsDTR.Add(DBID, runtimeID);
            UnitsRTD.Add(runtimeID, DBID);
        }

        public static int GetOrderDBID(string runtimeID)
        {
            return OrdersRTD[runtimeID];
        }

        public static string GetOrderRuntimeID(int DBId)
        {
            return OrdersDTR[DBId];
        }

        public static int GetUnitDBID(string runtimeID)
        {
            return UnitsRTD[runtimeID];
        }

        public static string GetUnitRuntimeID(int DBId)
        {
            return UnitsDTR[DBId];
        }
    }
}
