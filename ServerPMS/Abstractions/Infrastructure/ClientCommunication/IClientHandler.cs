using System.Net.Sockets;
using ServerPMS.Infrastructure.ClientCommunication;
namespace ServerPMS.Abstractions.Infrastructure.ClientCommunication
{
	public interface IClientHandler
	{
        Task HandleClientAsync(CArray client);
    }
}

