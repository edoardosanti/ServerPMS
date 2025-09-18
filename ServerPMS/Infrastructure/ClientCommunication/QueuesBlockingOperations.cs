using System;
namespace ServerPMS.Infrastructure.ClientCommunication
{
	public enum QueuesBlockingOperations
	{
		MoveUp,
		MoveDown,
		Remove,
		DequeueLast,
		Enqueue,
	}
}

