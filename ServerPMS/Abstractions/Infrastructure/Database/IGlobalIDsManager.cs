using System;
namespace ServerPMS.Abstractions.Infrastructure.Database
{
	public interface IGlobalIDsManager
	{
        //methods
        bool RemoveOrderEntry(string runtimeID);
        bool RemoveUnitEntry(string runtimeID);
        void AddOrderEntry(string runtimeID, int DBID);
        void AddUserEntry(string runtimeID, int DBID);
        bool RemoveUserEntry(string runtimeID);
        string GetUserRuntimeID(int DBid);
        int GetUserDBID(string runtimeID);
        void AddUnitEntry(string runtimeID, int DBID);
        int GetOrderDBID(string runtimeID);
        string GetOrderRuntimeID(int DBId);
        int GetUnitDBID(string runtimeID);
        string GetUnitRuntimeID(int DBId);

    }
}

