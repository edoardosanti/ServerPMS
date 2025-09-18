using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Security.Authentication;
using ServerPMS.Abstractions.Infrastructure.ClientCommunication;
using Microsoft.Extensions.DependencyInjection;

//TODO: add logging on the whole class (if possible add loggers with filters) 

namespace ServerPMS.Infrastructure.ClientCommunication
{
    public class ClientHandler : IClientHandler
    {
        static Dictionary<string,ClientHandler> handlersTable;
        static ClientHandler()
        {
            handlersTable = new Dictionary<string, ClientHandler>();
        }
        static public HashSet<string> ConnectedHandlers => handlersTable.Keys.ToHashSet<string>();

        private static string Register(ClientHandler handler)
        {
            string id = Guid.NewGuid().ToString();
            handlersTable.Add(id,handler);
            return id;
        }

        private readonly ILogger<ClientHandler> logger;
        private readonly ICommandRouter router;
        private readonly X509Certificate2 certificate;
        private readonly IMessageFactory msgFactory;
        private readonly IServiceScope scope;

        private IConnectionHandler RequestChHandler;
        private IConnectionHandler FeedChHandler;
        private IConnectionHandler SystemChHandler;
        public readonly string ID;

        private NetworkStream reqNetStream;
        private NetworkStream feedNetStream;
        private NetworkStream sysNetStream;

        private SslStream reqSslStream;
        private SslStream feedSslStream;
        private SslStream sysSslStream;

        private Task requestsConnTask;
        private Task feedConnTask;
        private Task systemConnTask;

        private List<IConnectionHandler> cHandlers = new();

        public ClientHandler(ILogger<ClientHandler> logger, X509Certificate2 certificate, ICommandRouter router, IMessageFactory msgFactory, IServiceScope scope)
        {
            ID = Register(this);
            logger.LogInformation("New client registered {0}", ID);
            this.logger = logger;
            this.router = router;
            this.msgFactory = msgFactory;
            this.certificate = certificate;
            this.scope = scope;
           
            RequestChHandler = scope.ServiceProvider.GetRequiredService<IConnectionHandler>();
            RequestChHandler.NewMessageHandler += (o, e) => _ = OnNewMessageAsync(o as ConnectionHandler, e);
            cHandlers.Add(RequestChHandler);

            FeedChHandler = scope.ServiceProvider.GetRequiredService<IConnectionHandler>();
            FeedChHandler.NewMessageHandler += (o, e) => _ = OnNewMessageAsync(o as ConnectionHandler, e);
            cHandlers.Add(FeedChHandler);

            SystemChHandler = scope.ServiceProvider.GetRequiredService<IConnectionHandler>();
            SystemChHandler.NewMessageHandler += (o, e) => _ = OnNewMessageAsync(o as ConnectionHandler, e);
            cHandlers.Add(SystemChHandler);
        }

        public async Task HandleClientAsync(CArray client)
        {
            logger.LogInformation("Handling client {0}", ID);

            try
            {

                reqNetStream = client.req.GetStream();
                feedNetStream = client.feed.GetStream();
                sysNetStream = client.sys.GetStream();

                reqSslStream = new SslStream(
                    reqNetStream,
                    leaveInnerStreamOpen: false
                );

                feedSslStream = new SslStream(
                    feedNetStream,
                    leaveInnerStreamOpen: false
                );

                sysSslStream = new SslStream(
                    sysNetStream,
                    leaveInnerStreamOpen: false
                );

                // Authenticate server with certificate

                #region Authentication
                var reqAuth = reqSslStream.AuthenticateAsServerAsync(
                    certificate,
                    clientCertificateRequired: false,
                    enabledSslProtocols: SslProtocols.Tls12 | SslProtocols.Tls13,
                    checkCertificateRevocation: false
                );

                var feedAuth = feedSslStream.AuthenticateAsServerAsync(
                    certificate,
                    clientCertificateRequired: false,
                    enabledSslProtocols: SslProtocols.Tls12 | SslProtocols.Tls13,
                    checkCertificateRevocation: false
                );
                var sysAuth = sysSslStream.AuthenticateAsServerAsync(
                    certificate,
                    clientCertificateRequired: false,
                    enabledSslProtocols: SslProtocols.Tls12 | SslProtocols.Tls13,
                    checkCertificateRevocation: false
                );

                // errore genera qui
                Task.WaitAll(reqAuth, feedAuth, sysAuth);

                logger.LogInformation("[{0}] SSL Authentication..", ID);



                #endregion

                requestsConnTask = RequestChHandler.AttachConnection(reqSslStream, ConnRole.RequestsChannel,7000,true);
                logger.LogInformation($"[{ID}] Secure connection on requests channel established.");
                
                feedConnTask = FeedChHandler.AttachConnection(feedSslStream, ConnRole.FeedChannel,7000,true);
                logger.LogInformation("[{0}] Secure connection on feed channel established.", ID);

                systemConnTask = SystemChHandler.AttachConnection(sysSslStream, ConnRole.SysChannel,7000,true);
                logger.LogInformation("[{0}] Secure connection on Backup Channel established.", ID);


                //TODO: Implement connection health monitor (v2)

                await Task.WhenAll(requestsConnTask, feedConnTask, systemConnTask);

            }
            catch (Exception ex)
            {
                
                logger.LogError($"[{ID}] TLS error: {ex.Message} on streams.");
            }

            finally
            {
                logger.LogInformation($"[{ID}] Client Disconnected.");
                Dispose();
            }
        }

        private async Task EnqueueMessage(ConnRole role ,Message msg)
        {
            IConnectionHandler handler = cHandlers.Find(x => x.Role ==role);
            try
            {
                await handler.EnqueueMessageForSending(msg);
            }
            catch(NullReferenceException nrex)
            {
                throw new InvalidOperationException("A connection must be attached to enqueue message for sending.");
            }
            
        }

        public async Task EnqueueSystemMessage(Message msg)
        {
            await EnqueueMessage(ConnRole.SysChannel, msg);
        }

        public async Task EnqueueRequestMessage(Message msg)
        {
            await EnqueueMessage(ConnRole.RequestsChannel, msg);
        }

        public async Task EnqueueFeedMessage(Message msg)
        {
            await EnqueueMessage(ConnRole.FeedChannel, msg);
        }

        private async Task OnNewMessageAsync(ConnectionHandler handler, Message message)
        {
            try
            {
                Console.WriteLine("Handler:\n{0}\nMessage:\n{1}\n\n", handler.Info(), msgFactory.GetString(message));

                switch (handler.Role)
                {
                    case ConnRole.RequestsChannel:
                        _ = router.RouteRequestAsync(this, message);
                        break;
                    case ConnRole.FeedChannel:
                        if (message.CID.Equals(CID.NACK))
                            _ = router.RouteFeedNackAsync(this, message);
                        break;
                    case ConnRole.SysChannel:
                        _ = router.RouteSystemAsync(this, message);
                        break;
                }
            }catch(Exception ex)
            {
                logger.LogError("[{0}] ERR: {1}", ID, ex.Message);
            }
        }

        public void Dispose()
        {
            //ssl streams close

            try { reqSslStream.Close(); } catch { }
            try { reqSslStream.Dispose(); } catch { }

            try { feedSslStream.Close(); } catch { }
            try { feedSslStream.Dispose(); } catch { }

            try { sysSslStream.Close(); } catch { }
            try { sysSslStream.Dispose(); } catch { }

            //network streams close

            try { reqNetStream.Close(); } catch { }
            try { reqNetStream.Dispose(); } catch { }

            try { feedNetStream.Close(); } catch { }
            try { feedNetStream.Dispose(); } catch { }

            try { sysNetStream.Close(); } catch { }
            try { sysNetStream.Dispose(); } catch { }

            scope.Dispose();
        }
    }
}

