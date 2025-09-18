using System;
namespace ServerPMS.Infrastructure.Concurrency { 

	public record Snapshot
	{
		object value;
		DateTime timestamp;

		public Snapshot(object value)
		{
			this.value = value;
			timestamp = DateTime.Now;
		}
	}
}

