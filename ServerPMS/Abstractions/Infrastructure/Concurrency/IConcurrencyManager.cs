using System;
using ServerPMS.Infrastructure.Concurrency;
using ServerPMS.Infrastructure.ClientCommunication;
namespace ServerPMS.Abstractions.Infrastructure.Concurrency
{
	public interface IConcurrencyManager
    {
        void ReleaseResource(AccessToken token);

        Task<AccessToken> AccessResourceAsync(ClientHandler handler, string uniqueID);
        Task<AccessToken> AccessIEMAsync(ClientHandler handler);
        Task<AccessToken> AccessOrdersManagerAsync(ClientHandler handler);
    }
}

