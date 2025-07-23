using System.Net.Sockets;
namespace ServerPMS.Infrastructure.ClientCommunication
{
	public record CArray
	{
		public TcpClient cts;
		public TcpClient stc;
		public TcpClient bkp;
	}
}

