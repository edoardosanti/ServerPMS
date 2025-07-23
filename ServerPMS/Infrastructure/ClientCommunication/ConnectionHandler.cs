using System.IO;
using System.Net.Security;
using System.Text;
using ServerPMS.Abstractions.Infrastructure.ClientCommunication;

namespace ServerPMS.Infrastructure.ClientCommunication
{
	//todo: add loggger (di and actual logging logic)

	public class SSLConnectionHandler : IConnectionHandler, IDisposable
	{
		public event EventHandler<Message> MessageRecivedHandler;

		private SslStream Stream;
		private readonly IMessageFactory MsgFactory;

		public SSLConnectionHandler(SslStream stream,IMessageFactory msgFactory)
		{
			MsgFactory = msgFactory;
            Stream = stream;
            Task.Run(WorkerLoop);

        }

		public async Task SendMessageAsync(Message message)
		{
			byte[] responseBytes = MsgFactory.GetBytes(message);
            await Stream.WriteAsync(responseBytes, 0, responseBytes.Length);
        }

		private async Task WorkerLoop()
		{
            var buffer = new byte[4096];
            int bytesRead;
            while (Stream.IsAuthenticated)
            {
				bytesRead = await Stream.ReadAsync(buffer);
                Message msg = MsgFactory.GetMessage(buffer.AsSpan(0, bytesRead).ToArray());

                //Console.WriteLine(Encoding.UTF8.GetString(msg.Payload));

                OnMessageRecived(msg);

				_ = SendMessageAsync(MsgFactory.AckMessage());
				
            }
        }

		private void OnMessageRecived(Message message)
		{
			MessageRecivedHandler?.Invoke(this, message);
		}

		public void Dispose()
		{
			 Stream.Dispose();

		}
	}
}

