using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Hosting;
using ServerPMS.Abstractions.Core;
using ServerPMS.Abstractions.Infrastructure.Config;
using Microsoft.Extensions.DependencyInjection;
using ServerPMS.Abstractions.Infrastructure.ClientCommunication;
using ServerPMS.Infrastructure.ClientCommunication;

namespace ServerPMS
{

    class Application : BackgroundService
    {

        private readonly IAppCore _appCore;
        private TcpListener ctsListener;
        private TcpListener stcListener;
        private TcpListener bkpListener;
        private readonly IServiceProvider _serviceProvider;

        public Application(IAppCore core,IConfigCrypto configCrypto,IGlobalConfigManager globalConfigManager, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            //initializes all business logic
            _appCore = core;
            _appCore.InitializeWALEnviroment();
            _appCore.WALReplay();

            ctsListener = new TcpListener(IPAddress.Any, 5600);
            ctsListener.Start();

            stcListener = new TcpListener(IPAddress.Any, 5610);
            stcListener.Start();

            bkpListener = new TcpListener(IPAddress.Any, 5620);
            bkpListener.Start();

            Console.Clear();
            Console.WriteLine("Server started...");

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {

                CArray client = new CArray
                {
                    cts = await ctsListener.AcceptTcpClientAsync(),
                    stc = await stcListener.AcceptTcpClientAsync(),
                    bkp = await bkpListener.AcceptTcpClientAsync()
                };

                using var scope = _serviceProvider.CreateScope(); // <-- create DI scope for this connection
                var handler = scope.ServiceProvider.GetRequiredService<IClientHandler>();

                _ = handler.HandleClientAsync(client); // async fire-and-forget
            }

        }
    }
} 