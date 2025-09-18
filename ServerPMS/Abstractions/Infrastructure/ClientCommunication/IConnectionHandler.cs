using System;
using System.Net.Security;
using ServerPMS.Infrastructure.ClientCommunication;
namespace ServerPMS.Abstractions.Infrastructure.ClientCommunication
{
	public interface IConnectionHandler
	{
		//properties
		public ConnRole Role { get; }
		public event EventHandler<Message> NewMessageHandler;
		public bool IsConnected { get; }

		//methods
		public Task AttachConnection(SslStream stream, ConnRole role, int heartbeatIntervalMs = 5000, bool filterHeartbeat=false);
		public Task EnqueueMessageForSending(Message message);
		public void ChangeRole(ConnRole newRole);

    }
}

