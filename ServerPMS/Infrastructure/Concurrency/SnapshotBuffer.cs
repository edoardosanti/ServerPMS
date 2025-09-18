using ServerPMS.Abstractions.Infrastructure.Concurrency;
namespace ServerPMS.Infrastructure.Concurrency
{
    public class SnapshotBufferMissException : Exception
    {
        public SnapshotBufferMissException(string resource) : base(string.Format("No snapshot avaible for mapped resource: {0}", resource)) { }
    }

    public class SnapshotBuffer : ISnapshotBuffer
	{
        private Dictionary<string, Snapshot> snapshotTable;
        private Dictionary<string, bool> refTable;

		public SnapshotBuffer()
		{
            snapshotTable = new Dictionary<string, Snapshot>();
            refTable = new Dictionary<string, bool>();
		}

		public Snapshot? GetLastSnapshot(string resourceID)
		{
            if (!snapshotTable.TryGetValue(resourceID, out Snapshot snap))
                return null;
            return snap;
		}

        public void NewSnapshot(string uniqueResourceId, object value)
        {
            snapshotTable.Add(uniqueResourceId, new Snapshot(value));
        }

        public Snapshot GetSnapshot(string uniqueResourceId, DateTime timestamp)
        {
            throw new NotImplementedException();
        }
    }
}

