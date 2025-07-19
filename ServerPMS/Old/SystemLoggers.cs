using ServerPMS.Abstractions.Infrastructure.Logging;
using Microsoft.Extensions.Logging;

namespace ServerPMS.Old
{
    public class SystemLoggers : ISystemLoggers
    {
        public ILogger DBAs { get; private set; }
        public ILogger Queues { get; private set; }
        public ILogger Orders { get; private set; }
        public ILogger Units { get; private set; }
        public ILogger Core { get; private set; }

        public SystemLoggers(ILoggerFactory factory)
        {
            DBAs = factory.CreateLogger("DBAs");
            Queues = factory.CreateLogger("Queues");
            Orders = factory.CreateLogger("Orders");
            Units = factory.CreateLogger("Units");
            Core = factory.CreateLogger("Core");

        }

    }
}

