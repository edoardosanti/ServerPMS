using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using ServerPMS.Abstractions.Infrastructure.ClientCommunication;

//TODO: add logging on the whole class (if possible add loggers with filters) 

namespace ServerPMS.Infrastructure.ClientCommunication
{
    public class SSLClientHandler : IClientHandler
    {
        static Dictionary<string,SSLClientHandler> handlersTable;
        static SSLClientHandler()
        {
            handlersTable = new Dictionary<string, SSLClientHandler>();
        }

        private static string Register(SSLClientHandler handler)
        {
            string id = Guid.NewGuid().ToString();
            handlersTable.Add(id,handler);
            return id;
        }

        private readonly ILogger<SSLClientHandler> Logger;
        private readonly ICommandRouter Router;
        private readonly X509Certificate2 Certificate;
        private readonly IMessageFactory MsgFactory;

        private SSLConnectionHandler CTSConnHandler;
        private SSLConnectionHandler STCConnHandler;
        private SSLConnectionHandler BKPConnHandler;


        public readonly string ID;

        public SSLClientHandler(ILogger<SSLClientHandler> logger, X509Certificate2 certificate, ICommandRouter router, IMessageFactory msgFactory)
        {
            ID = Register(this);
            Logger = logger;
            Router = router;
            MsgFactory = msgFactory;
            Certificate = certificate;

        }

        public async Task HandleClientAsync(CArray client)
        {
            var ctsNetStream = client.cts.GetStream();
            var stcNetStream = client.stc.GetStream();
            var bkpNetStream = client.bkp.GetStream();

            var ctsSslStream = new SslStream(
                ctsNetStream,
                leaveInnerStreamOpen: false
            );

            var stcSslStream = new SslStream(
                stcNetStream,
                leaveInnerStreamOpen: false
            );

            var bkpSslStream = new SslStream(
                bkpNetStream,
                leaveInnerStreamOpen: false
            );

            try
            {
                // Authenticate server with certificate
                var ctsAuth = ctsSslStream.AuthenticateAsServerAsync(
                    Certificate,
                    clientCertificateRequired: false,
                    enabledSslProtocols: SslProtocols.Tls12 | SslProtocols.Tls13,
                    checkCertificateRevocation: false
                );

                var stcAuth = stcSslStream.AuthenticateAsServerAsync(
                    Certificate,
                    clientCertificateRequired: false,
                    enabledSslProtocols: SslProtocols.Tls12 | SslProtocols.Tls13,
                    checkCertificateRevocation: false
                );
                var bkpAuth = bkpSslStream.AuthenticateAsServerAsync(
                    Certificate,
                    clientCertificateRequired: false,
                    enabledSslProtocols: SslProtocols.Tls12 | SslProtocols.Tls13,
                    checkCertificateRevocation: false
                );

                await Task.WhenAll(ctsAuth, stcAuth, bkpAuth);


                CTSConnHandler = new SSLConnectionHandler(ctsSslStream, MsgFactory);
                CTSConnHandler.MessageRecivedHandler += (s, m) => { Console.WriteLine("CTS: {0}", Encoding.UTF8.GetString(m.Payload)); };
                Console.WriteLine("Secure connection CTS established with client {0} ", ID);

                STCConnHandler = new SSLConnectionHandler(stcSslStream, MsgFactory);
                STCConnHandler.MessageRecivedHandler += (s, m) => { Console.WriteLine("STC: {0}", Encoding.UTF8.GetString(m.Payload)); };
                Console.WriteLine("Secure connection STC established with client {0} ", ID);

                BKPConnHandler = new SSLConnectionHandler(bkpSslStream, MsgFactory);
                BKPConnHandler.MessageRecivedHandler += (s, m) => { Console.WriteLine("BKP: {0}", Encoding.UTF8.GetString(m.Payload)); };
                Console.WriteLine("Secure connection BKP established with client {0} ", ID);


            }
            catch (Exception ex)
            {
                Console.WriteLine($"TLS error: {ex.Message} on CTS stream.");
            }

            Console.WriteLine("Client disconnected.");
        }
    }
}

