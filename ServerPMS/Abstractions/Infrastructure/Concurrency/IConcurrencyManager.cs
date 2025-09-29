using System;
using ServerPMS.Infrastructure.Concurrency;
using ServerPMS.Infrastructure.ClientCommunication;
namespace ServerPMS.Abstractions.Infrastructure.Concurrency
{
	public interface IConcurrencyManager
    {
        List<AccessToken> ActiveTokens { get; }

        void ReleaseResource(AccessToken token);

        Task<AccessToken> AccessResourceAsync(ClientHandler handler, string uniqueID);
        Task<AccessToken> AccessIEMAsync(ClientHandler handler);
        Task<AccessToken> AccessIEMAsync(string handlerID);
        Task<AccessToken> AccessOrdersManagerAsync(ClientHandler handler);
        Task<AccessToken> AccessOrdersManagerAsync(string ID);
        Task<AccessToken> AccessUnitsManagerAsync(ClientHandler handler);
        Task<AccessToken> AccessUnitsManagerAsync(string ID);
        Task<AccessToken> AccessQueuesManagerAsync(ClientHandler handler);
        Task<AccessToken> AccessQueuesManagerAsync(string ID);
    }
}

