using ServerPMS.Infrastructure.Concurrency;
namespace ServerPMS.Abstractions.Infrastructure.Concurrency
{
	public interface ISnapshotBuffer
	{
		Snapshot GetLastSnapshot(string uniqueResourceId);
		Snapshot? GetSnapshot(string uniqueResourceId, DateTime timestamp);
		void NewSnapshot(string resourceID, object value);

    }
}

