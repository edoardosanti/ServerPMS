using System;
namespace ServerPMS.Abstractions.Managers
{
	public interface IIntegratedEventsManager
	{

        //methods
        void Enqueue(string queueRuntimeID, string orderRuntimeID);
        void MoveUpInQueue(string queueRuntimeID, string orderRuntimeID, int steps);
        void MoveDownInQueue(string queueRuntimeID, string orderRuntimeID, int steps);
        void MoveInQueue(string queueRuntimeID, string orderRuntimeID, int steps);
        string DequeueAndComplete(string queueRuntimeID);
        int PositionOf(string queueRuntimeID, string orderRuntimeID);
        string FindInQueue(string orderRuntimeID);
        void RemoveFromQueueNotEOF(string queueRuntimeID, string orderRuntimeID);

    }
}

