using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Hosting;
using ServerPMS.Abstractions.Core;
using ServerPMS.Abstractions.Infrastructure.Config;

namespace ServerPMS
{

    class Application : BackgroundService
    {

        private readonly IAppCore Core;
        private TcpListener tcpListener;


        public Application(IAppCore core,IConfigCrypto configCrypto,IGlobalConfigManager globalConfigManager)
        {
            Core = core;

            tcpListener = new TcpListener(IPAddress.Any, 5600);
            tcpListener.Start();

            Console.Clear();
            Console.WriteLine("Server started...");

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await tcpListener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client);
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            using var stream = client.GetStream();
            var buffer = new byte[1024];
            int read;

            Console.WriteLine("Client connected.");

            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, read).Trim();

                if (message.Equals("INCR", StringComparison.OrdinalIgnoreCase))
                {
                    int newVal = 0;
                    var response = $"Counter: {newVal}\n";
                    var responseBytes = Encoding.UTF8.GetBytes(response);
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                }
            }

            Console.WriteLine("Client disconnected.");
        }
    }
} 