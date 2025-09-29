using System.Net.Sockets;
using ServerPMS.Infrastructure.ClientCommunication;
namespace ServerPMS.Abstractions.Infrastructure.ClientCommunication
{
	public interface IClientHandler
	{
        string ID { get; }
        Task HandleClientAsync(CArray client);
    }
}

