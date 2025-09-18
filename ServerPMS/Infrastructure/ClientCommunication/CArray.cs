using System.Net.Sockets;
namespace ServerPMS.Infrastructure.ClientCommunication
{
	public record CArray
	{
		public TcpClient req;
		public TcpClient feed;
		public TcpClient sys;
	}
}

