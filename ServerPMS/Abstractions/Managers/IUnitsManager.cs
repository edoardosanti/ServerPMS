using System;
namespace ServerPMS.Abstractions.Managers
{
	public interface IUnitsManager
	{
		
        ProductionUnit this[string runtimeId] { get; }
		IEnumerable<string> IDs { get; }

        //methods
        Task LoadUnitsAsync();
		void Start(string runtimeID);
		void Stop(string runtimeID);
		string? GetName(string runtimeID);
		string? GetNotes(string runtimeID);


    }
}

