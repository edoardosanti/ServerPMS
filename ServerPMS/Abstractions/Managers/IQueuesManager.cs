using ServerPMS.Infrastructure.Generic;

namespace ServerPMS.Abstractions.Managers
{
    public interface IQueuesManager
	{
		//properties
        UnitQueue this[string runtimeId] { get; }
		IEnumerable<string> IDs { get; }

		//methods
        string NewQueue(string bindToUnit);
        Task LoadQueueAsync(string runtimeID);
		void LoadQueue(string runtimeID);
		void LoadAll();
		Task LoadAllAsync();

	}
}

