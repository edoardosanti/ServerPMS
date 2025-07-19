using ServerPMS.Infrastructure.Generic;

namespace ServerPMS.Abstractions.Managers
{
    public interface IQueuesManager
	{
		//properties
        ReorderableQueue<string> this[string runtimeId] { get; }
		IEnumerable<string> IDs { get; }

		//methods
        void NewQueue(string runtimeID);
		Task LoadQueueAsync(string runtimeID);
		void LoadQueue(string runtimeID);
		void LoadAll();
		void LoadAllAsync();

	}
}

