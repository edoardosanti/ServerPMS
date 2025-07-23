using ServerPMS.Abstractions.Infrastructure.ClientCommunication;
using ServerPMS.Abstractions.Core;
namespace ServerPMS.Infrastructure.ClientCommunication
{
	public class CommandRouter: ICommandRouter
	{
		private readonly IAppCore Core;

		public CommandRouter(IAppCore core)
		{
			Core = core;
		}

		public async Task RouteAsync(SSLClientHandler client, Message cmd)
		{

		}
	}
}

