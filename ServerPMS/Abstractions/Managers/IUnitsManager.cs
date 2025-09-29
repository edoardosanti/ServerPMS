using System;
using ServerPMS.Infrastructure.ClientCommunication;

namespace ServerPMS.Abstractions.Managers
{
	public interface IUnitsManager:IEnumerable<ProductionUnit>
	{
		
        ProductionUnit this[string runtimeId] { get; }
		IEnumerable<string> IDs { get; }

        //methods
        Task LoadUnitsAsync();
		void Start(string runtimeID, string userID);
		void Stop(string runtimeID,string userID);
		string? GetName(string runtimeID);
		string? GetNotes(string runtimeID);


    }
}

