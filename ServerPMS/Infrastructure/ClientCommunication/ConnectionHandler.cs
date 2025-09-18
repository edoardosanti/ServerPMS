using System.Text;
using System.Net.Security;
using System.Timers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using ServerPMS.Abstractions.Infrastructure.ClientCommunication;

namespace ServerPMS.Infrastructure.ClientCommunication
{

	public record OutboxEntry
	{
		public Message Message;
		public TaskCompletionSource CompletionSource { get; } = new();
	}


	public class ConnectionHandler : IConnectionHandler, IDisposable
	{

		private SslStream stream;
		public ConnRole Role { private set; get; }

		private readonly IMessageFactory msgFactory;
		private readonly ILogger<ConnectionHandler> logger;
		private StreamParser reader;

        private DateTime lastHeartbeatTimestamp;
		private object lastHeartbeatLock =  new();
		private int heartbeatMaxIntervalMs;
		private System.Timers.Timer heartbeatTimer;
		private bool filterSystemMessages;

		public event EventHandler<Message> NewMessageHandler;

		private CancellationTokenSource dscSrc;
		public bool IsConnected { private set; get; }

		private Queue<OutboxEntry> outboxBuffer;

		private Task mainLoopTask;

		private TaskCompletionSource connTask;

		public ConnectionHandler(ILogger<ConnectionHandler> logger, IMessageFactory msgFactory)
		{

			this.msgFactory = msgFactory;
			this.logger = logger;
			IsConnected = false;
			outboxBuffer = new Queue<OutboxEntry>();
			connTask = new TaskCompletionSource();
		}


        public Task AttachConnection(SslStream stream, ConnRole role, int heartbeatIntervalMs, bool filterSystemMessages)
        {
            this.stream = stream;
            Role = role;
			this.filterSystemMessages = filterSystemMessages;
			IsConnected = true;
			reader = new StreamParser(stream);

			//heartbeat logic
			lastHeartbeatTimestamp = DateTime.UtcNow;
            heartbeatMaxIntervalMs = heartbeatIntervalMs;
            SetupHeartbeatCheck();

            dscSrc = new CancellationTokenSource();
            mainLoopTask = Task.Run(() => MainLoop(dscSrc.Token));
			SendSetRoleRequest(Role);
			return connTask.Task;

        }

        private async Task SendMessageAsync(Message msg)
		{
			Console.WriteLine("Sending message {0}", msgFactory.GetString(msg));

			try
			{
				byte[] bytes = msgFactory.Serialize(msg);
				await stream.WriteAsync(bytes);
			}
			catch(IOException ioex)
			{
				Console.WriteLine("Connection has been shut down. No more messages can be routed through this channel.");
			}
		}

		public Task EnqueueMessageForSending(Message message)
		{
			
			Console.WriteLine("Enqueueing message: {0}",msgFactory.GetString(message));


			if (!IsConnected)
			{
				throw new InvalidOperationException("Connection has been shut down. No more messages can be routed through this channel.");
			}
			OutboxEntry entry = new OutboxEntry { Message = message };
			outboxBuffer.Enqueue(entry);
			return entry.CompletionSource.Task;
		}

		private async Task MainLoop(CancellationToken ctk)
		{
			try
			{
				while (!ctk.IsCancellationRequested)
				{
					//send messages in buffer
					while (outboxBuffer.TryDequeue(out OutboxEntry entry))
					{
						Console.WriteLine("Sending message");
						await SendMessageAsync(entry.Message);
						entry.CompletionSource.SetResult();
					}

					byte[] messageBytes = await reader.GetMessageBytesAsync();
					Message msg = msgFactory.Deserialize(messageBytes);

					switch(msg.CID)
					{
						case (byte)CID.HEARTBEAT:
						UpdateLastHeartbeatTimestamp(DateTime.UtcNow);
						if (!filterSystemMessages)
							OnNewMessage(msg);
							break;

						case (byte)CID.LOGOUT:
                            if (!filterSystemMessages)
                                OnNewMessage(msg);
                            Dispose();
							break;
						default:
                            OnNewMessage(msg);
							break;
                    }
					await SendMessageAsync(msgFactory.AckMessage(msg.ID));
				}
			}
			catch (IOException ioex)
			{
				Console.WriteLine("{0} disconnected.", Role.ToString());
			}
			finally
 			{
				Dispose();
			}
		}

		private void OnNewMessage(Message message)
		{
			NewMessageHandler?.Invoke(this, message);
		}

		public void SendSetRoleRequest(ConnRole role)
		{
            switch (role)
            {
                case ConnRole.RequestsChannel:
                    EnqueueMessageForSending(msgFactory.NewMessage(0x84, 0x00, "RR"));
                    break;
                case ConnRole.FeedChannel:
                    EnqueueMessageForSending(msgFactory.NewMessage(0x84, 0x00, "RF"));
                    break;
                case ConnRole.SysChannel:
                    EnqueueMessageForSending(msgFactory.NewMessage(0x84, 0x00, "RB"));
                    break;
                default:
					break;
            }
        }

        private void UpdateLastHeartbeatTimestamp(DateTime last)
        {
            lock (lastHeartbeatLock)
            {
                lastHeartbeatTimestamp=last;
            }
        }

        public DateTime GetLastHeartbeatTimestamp()
		{
			lock (lastHeartbeatLock)
			{
				return lastHeartbeatTimestamp;
			}
		}

		public void ChangeRole(ConnRole newRole)
		{
			Role = newRole;
			SendSetRoleRequest(Role);
		}

		private void SetupHeartbeatCheck()
		{
			heartbeatTimer = new System.Timers.Timer(heartbeatMaxIntervalMs);
			heartbeatTimer.Elapsed += CheckHeartbeatCadence;
            heartbeatTimer.Start();

        }

		private void CheckHeartbeatCadence(object? sender, ElapsedEventArgs e)
		{
			if (GetLastHeartbeatTimestamp() < (DateTime.UtcNow - TimeSpan.FromMilliseconds(heartbeatMaxIntervalMs * 1.33)))
			{
				Console.WriteLine("Heartbeat missing. Disconnecting ({0})",Role.ToString());
				heartbeatTimer.Stop();
                Dispose();
			}
		}

		public string Info()
		{
			return string.Format("Role: {0}\tLast heartbeat at: {1}\t Connected: {2}", Role.ToString(), lastHeartbeatTimestamp.ToLongTimeString(), IsConnected);
		}

		public void Dispose()
		{
			heartbeatTimer?.Stop();
            IsConnected = false;
            dscSrc.Cancel();
            dscSrc.Dispose();
            connTask.SetResult();
        }
    }
}
