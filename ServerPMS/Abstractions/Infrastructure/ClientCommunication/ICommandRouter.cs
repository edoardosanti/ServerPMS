using ServerPMS.Infrastructure.ClientCommunication;
namespace ServerPMS.Abstractions.Infrastructure.ClientCommunication
{
	public interface ICommandRouter
	{
		Task RouteRequestAsync(ClientHandler handler, Message cmd);
        Task RouteFeedNackAsync(ClientHandler handler, Message cmd);
		Task RouteSystemAsync(ClientHandler handler, Message cmd);

    }
}

