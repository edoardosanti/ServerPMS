using System;
using Microsoft.Extensions.Logging;

namespace ServerPMS
{
    public static class Loggers
    {
        public static ILogger DBAs { get; private set; }
        public static ILogger Queues { get; private set; }
        public static ILogger Orders { get; private set; }
        public static ILogger Units { get; private set; }
        public static ILogger Core { get; private set; }

        public static void Init(ILoggerFactory factory)
        {
            DBAs = factory.CreateLogger("DBAs");
            Queues = factory.CreateLogger("Queues");
            Orders = factory.CreateLogger("Orders");
            Units = factory.CreateLogger("Units");
            Core = factory.CreateLogger("Core");

        }
    }
}

