using System.Net;
using System.Net.Sockets;
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
        private TcpListener reqListener;
        private TcpListener feedListener;
        private TcpListener sysListener;
        private readonly IServiceProvider _serviceProvider;

        private Task loop;
        private object stateLock;
        private bool state;


        public Application(IAppCore core,IConfigCrypto configCrypto,IGlobalConfigManager globalConfigManager, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _appCore = core;
            stateLock = new();
            state = false;

        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {

            //initializes all business logic
            try
            {
                await _appCore.InitializeManagersAsync();
                _appCore.InitializeWALEnviroment();
                Task walReplayTask = _appCore.WALReplayAsync();

                reqListener = new TcpListener(IPAddress.Any, 56000);
                reqListener.Start();

                feedListener = new TcpListener(IPAddress.Any, 56100);
                feedListener.Start();

                sysListener = new TcpListener(IPAddress.Any, 56200);
                sysListener.Start();


                await walReplayTask;
                Console.WriteLine("Application start async called.");

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in application start: {0}", ex.Message); //-> log 
            }

             base.StartAsync(cancellationToken);

        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            while (!GetState())
            {
                await Task.Delay(100);
            }
            loop.Dispose();
        }

        private void SetState(bool newState)
        {
            lock (stateLock)
            {
                state = newState;
            }
        }

        private bool GetState()
        {
            lock (stateLock)
            {
                return state ;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Application execute async called");
            try
            {
                Console.WriteLine("Server started...");

                while (true)
                {
                    Console.WriteLine("Handlers loop iteration start");
                    Task<TcpClient> reqTask = reqListener.AcceptTcpClientAsync();
                    Task<TcpClient> feedTask = feedListener.AcceptTcpClientAsync();
                    Task<TcpClient> systemTask = sysListener.AcceptTcpClientAsync();

                    await Task.WhenAll(reqTask, feedTask, systemTask);

                    SetState(true);

                    CArray client = new()
                    {
                        req = reqTask.Result,
                        feed = feedTask.Result,
                        sys = systemTask.Result
                    };

                    IServiceScope scope = _serviceProvider.CreateScope(); // <-- create DI scope for this connection
                    IClientHandler handler = ActivatorUtilities.CreateInstance<ClientHandler>(scope.ServiceProvider, scope);
                    _ = handler.HandleClientAsync(client);
                    Console.WriteLine("Handlers loop iteration end");

                    SetState(false);
                }
            }
            catch (Exception ex)
            {
                SetState(false);
                Console.WriteLine("Errore in Application.cs nella creazioned di nuovo client handler: {0}\n{1}", ex.Message, ex.StackTrace);
                await StopAsync(stoppingToken);

            }
        }
    }
} 