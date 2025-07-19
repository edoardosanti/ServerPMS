using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Filters;
using Serilog.Sinks.SystemConsole.Themes;
using ServerPMS.Abstractions.Core;
using ServerPMS.Abstractions.Infrastructure.Config;
using ServerPMS.Abstractions.Infrastructure.Database;
using ServerPMS.Abstractions.Infrastructure.External;
using ServerPMS.Abstractions.Infrastructure.Logging;
using ServerPMS.Abstractions.Managers;
using ServerPMS.Core;
using ServerPMS.Infrastructure.Config;
using ServerPMS.Infrastructure.Database;
using ServerPMS.Infrastructure.External;
using ServerPMS.Infrastructure.Logging;
using ServerPMS.Managers;

namespace ServerPMS
{
    internal class Program
    {

        static async Task Main(string[] args)
        {

            //setup serilog logger
            Serilog.Core.Logger logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                // DBAs
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(Matching.FromSource("ServerPMS.Infrastructure.Database.CommandDBAccessor"))
                    .WriteTo.File("logs/cdba.log", rollingInterval: RollingInterval.Day,
                                  outputTemplate: "[{Timestamp:HH:mm:ss} CDBA] {Message}{NewLine}")
                )
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(Matching.FromSource("ServerPMS.Infrastructure.Database.QueryDBAccessor"))
                    .WriteTo.File("logs/qdba.log", rollingInterval: RollingInterval.Day,
                                  outputTemplate: "[{Timestamp:HH:mm:ss} QDBA] {Message}{NewLine}")
                )
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(Matching.FromSource("ServerPMS.Managers.QueuesManager"))
                    .WriteTo.File("logs/queues.log", rollingInterval: RollingInterval.Day,
                                  outputTemplate: "[{Timestamp:HH:mm:ss} QUEUES] {Message}{NewLine}")
                )
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(Matching.FromSource("ServerPMS.Managers.OrdersManager"))
                    .WriteTo.File("logs/orders.log", rollingInterval: RollingInterval.Day,
                                  outputTemplate: "[{Timestamp:HH:mm:ss} ORDERS] {Message}{NewLine}")
                )
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(Matching.FromSource("ServerPMS.Managers.UnitsManager"))
                    .WriteTo.File("logs/orders.log", rollingInterval: RollingInterval.Day,
                                  outputTemplate: "[{Timestamp:HH:mm:ss} UNITS] {Message}{NewLine}")
                )
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(Matching.FromSource("ServerPMS.Core.AppCore"))
                    .WriteTo.File("logs/core.log", rollingInterval: RollingInterval.Day,
                                  outputTemplate: "[{Timestamp:HH:mm:ss} CORE] {Message}{NewLine}")
                )
                .WriteTo.Logger(lc => lc
                    .WriteTo.File("logs/systemwide.log", rollingInterval: RollingInterval.Day,
                                  outputTemplate: "[{Timestamp:HH:mm:ss} SYSTEM] {Message}{NewLine}")
                )
                .CreateLogger();

            // Set global Serilog
            Log.Logger = logger;

            try
            {

                HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

                Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");

                #region CORE
                builder.Services.AddSingleton<IAppCore, AppCore>();
                #endregion

                #region INFRASTRUCUTURE

                // Use Serilog for Microsoft.Extensions.Logging
                builder.Logging.ClearProviders(); // optional, remove default loggers
                builder.Logging.AddSerilog(logger, dispose: true);

                // Register system loggers
                builder.Services.AddSingleton<ILoggerFactory>(new SerilogLoggerFactory(logger));
                builder.Services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

                // Register WAL Logger
                builder.Services.AddSingleton<IWALLogger, WALLogger>();

                //registers dba and db helpers
                builder.Services.AddSingleton<ICommandDBAccessor, CommandDBAccessor>();
                builder.Services.AddSingleton<IQueryDBAccessor, QueryDBAccessor>(); //TODO:make QBDA transient
                builder.Services.AddSingleton<IGlobalIDsManager, GlobalIDsManager>();

                //register configuration services
                builder.Services.AddSingleton<IConfigCrypto, ConfigCrypto>();
                builder.Services.AddSingleton<IGlobalConfigManager, GlobalConfigManager>();

                //register external services
                builder.Services.AddTransient<IExcelOrderParser, ExcelOrderParser>();
                #endregion

                #region MANAGERS
                //register order manager
                builder.Services.AddSingleton<IOrdersManager, OrdersManager>();

                //register units manager
                builder.Services.AddSingleton<IUnitsManager, UnitsManager>();

                //register queues manager
                builder.Services.AddSingleton<IQueuesManager, QueuesManager>();

                //register iem
                builder.Services.AddSingleton<IIntegratedEventsManager, IntegratedEventsManager>();
                #endregion

                #region CHECK AND MAIN SERVICE
                //register startup checks
                builder.Services.AddSingleton<StartupChecksService>();

                //register main service
                builder.Services.AddHostedService<Application>();
                #endregion

                IHost host = builder.Build();

                //run startup checks (if needed throw exception) 
                using (var scope = host.Services.CreateScope())
                {
                    var checks = scope.ServiceProvider.GetRequiredService<StartupChecksService>();
                    await checks.RunChecksAsync(); // This will work correctly now
                }

                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}

