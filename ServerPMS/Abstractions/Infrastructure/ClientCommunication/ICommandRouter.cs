using System;
using ServerPMS.Infrastructure.ClientCommunication;
namespace ServerPMS.Abstractions.Infrastructure.ClientCommunication
{
	public interface ICommandRouter
	{
		Task RouteAsync(SSLClientHandler handler, Message cmd);
	}
}

