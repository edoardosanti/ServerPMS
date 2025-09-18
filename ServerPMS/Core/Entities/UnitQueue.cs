using System;
using ServerPMS.Infrastructure.Generic;
namespace ServerPMS
{
	public class UnitQueue : ReorderableQueue<string>
	{
		public readonly string Id;
		public string BindedUnitID { private set; get; }
		public bool IsBinded
		{
			get
			{
				if (BindedUnitID == null)
					return false;
				else
					return true;
			}
		}

		public void SetBindedUnitID(string unitID)
		{
			BindedUnitID = unitID;
		}

		public UnitQueue(string Id) : base() { this.Id = Id; }

        public UnitQueue(string Id,string bindTo) : base()
		{
			this.Id = Id;
			BindedUnitID = bindTo;
		}

        public UnitQueue(IEnumerable<string> items) : base(items) {}

        public override string ToInfo()
        {
            return string.Format("Queue ID:{0}\t Binded Unit ID: {1}\n\nQueue:\n{3}",Id, BindedUnitID,base.ToInfo());
        }
    }
}

