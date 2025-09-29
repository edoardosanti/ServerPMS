using System;
using System.Linq;
using System.Text;
using ServerPMS.Infrastructure.Concurrency;
namespace ServerPMS.Infrastructure.ClientCommunication
{
    public abstract class Report
    {
        public string Dump { private set; get; }

        public bool IsAllLive { private set; get; }
        public string SnapIDsDump { private set; get; }

        public readonly DateTime Timestamp;

        public Report(string dump,bool allLive, string snapIDsDump)
        {
            if (!allLive)
                SnapIDsDump = snapIDsDump;
            else
                SnapIDsDump = string.Empty;

            Dump = dump;
            IsAllLive = allLive;
            Timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            return Dump + "#" + SnapIDsDump;
        }


        protected static string BuildSnapDump(IEnumerable<AccessToken>? list)
        {
            if (list != null)
            {
                var sb = new StringBuilder();
                foreach (var o in list)
                    sb.Append(o?.ResourceID);
                return sb.ToString() + "$";
            }
            return string.Empty;
        }

    }

    public sealed class OrdersReport : Report
	{
        private static string BuildOrdersDump(IEnumerable<ProductionOrder> list)
        {
            var sb = new StringBuilder();
            sb.Append($"{list.Count()}|");
            foreach (var o in list)
                sb.Append(o.ToString()).Append("|");
            return sb.ToString();
        }

        public OrdersReport(IEnumerable<ProductionOrder> orders, bool allLive, IEnumerable<AccessToken>? snapIDs=null) : base(BuildOrdersDump(orders), allLive, BuildSnapDump(snapIDs)) { }        
	}

    public sealed class UnitsReport : Report
    {
        public int OrdersCount { private set; get; }

        private static string BuildUnitsDump(IEnumerable<ProductionUnit> list)
        {
            var sb = new StringBuilder();
            foreach (var o in list)
                sb.Append(o.ToString()).Append("|");
            return sb.ToString();
        }

        public UnitsReport(IEnumerable<ProductionUnit> units, bool allLive, IEnumerable<AccessToken>? snapIDs = null) : base(BuildUnitsDump(units), allLive, BuildSnapDump(snapIDs)) { }
    }

    public sealed class QueuesReport : Report
    {
        public int OrdersCount { private set; get; }

        private static string BuildQueuesDump(IEnumerable<UnitQueue> list)
        {
            var sb = new StringBuilder();
            foreach (var o in list)
                sb.Append(o.ToString()).Append("|");
            return sb.ToString();
        }

        public QueuesReport(IEnumerable<UnitQueue> queues, bool allLive, IEnumerable<AccessToken>? snapIDs = null) : base(BuildQueuesDump(queues), allLive, BuildSnapDump(snapIDs)) { }
    }
}

