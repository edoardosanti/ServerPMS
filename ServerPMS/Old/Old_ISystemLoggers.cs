
using Microsoft.Extensions.Logging;

namespace ServerPMS.Old
{
	public interface ISystemLoggers
	{
        //properties
        ILogger DBAs { get; }
        ILogger Queues { get; }
        ILogger Orders { get; }
        ILogger Units { get; }
        ILogger Core { get; }

    }
}

