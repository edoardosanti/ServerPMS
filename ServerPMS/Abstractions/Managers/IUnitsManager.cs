using System;
namespace ServerPMS.Abstractions.Managers
{
	public interface IUnitsManager
	{
		Dictionary<string, ProductionUnit> Units { get; }

        //methods
        void LoadUnits();
		void Start(string runtimeID);
		void Stop(string runtimeID);
		
	}
}

