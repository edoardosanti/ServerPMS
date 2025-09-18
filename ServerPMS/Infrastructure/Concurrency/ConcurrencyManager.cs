using ServerPMS.Abstractions.Infrastructure.Concurrency;
using ServerPMS.Abstractions.Infrastructure.Config;
using ServerPMS.Infrastructure.ClientCommunication;
using ServerPMS.Abstractions.Core;
using ServerPMS.Abstractions.Managers;
using Microsoft.Extensions.Logging;

namespace ServerPMS.Infrastructure.Concurrency {

    public enum AccessMode
    {
        ReadOnly,
        ReadWrite,
    }

    public class ConcurrencyManager : IConcurrencyManager
    {
        private readonly ILogger<ConcurrencyManager> logger;
        private readonly IOrdersManager ordersManager;
        private readonly IGlobalConfigManager configManager;
        private readonly ISnapshotBuffer snapshotBuffer;
        private readonly IIntegratedEventsManager iem;
        private readonly IResourceMapper mapper;

        private HashSet<AccessToken> tokens;

        private SemaphoreSlim IEMSemaphore;
        private SemaphoreSlim CoreSemaphore;

        public ConcurrencyManager(ILogger<ConcurrencyManager> logger, IGlobalConfigManager configManager, ISnapshotBuffer snapshotBuffer, IAppCore core, IResourceMapper resourceMapper)
        {
            iem = core.IEM;
            ordersManager = core.OrdersManager;
            this.logger = logger;
            this.configManager = configManager;
            this.snapshotBuffer = snapshotBuffer;
            mapper = resourceMapper;
            tokens = new HashSet<AccessToken>();
            IEMSemaphore = new SemaphoreSlim(1);
            CoreSemaphore = new SemaphoreSlim(1);

        }

        private void LockResource(string uniqueID,object value)
        {
           snapshotBuffer.NewSnapshot(uniqueID,value);
        }

        public async Task<AccessToken> AccessIEMAsync(ClientHandler handler)
        {
            //wait for iem
            await IEMSemaphore.WaitAsync();

            //prepare token
            AccessToken at = new AccessToken(handler.ID, AccessMode.ReadWrite, iem);
            tokens.Add(at);

            return at;
        }

        public async Task<AccessToken> AccessOrdersManagerAsync(ClientHandler handler)
        {
            //wait for iem
            await CoreSemaphore.WaitAsync();

            //prepare token
            AccessToken at = new AccessToken(handler.ID, AccessMode.ReadWrite, ordersManager);
            tokens.Add(at);

            return at;
        }

        private bool IsSnapshotReferenced(Snapshot snap)
        {
            if (snap == null)
                return false;

            foreach(AccessToken at in tokens)
            {
                if (at.Snapshot == snap)
                    return true;
            }
            return false;
        }

        public async Task<AccessToken> AccessResourceAsync(ClientHandler handler, string uniqueID) //TODO: implemtare v2 con resource mapper e accesso singolo ai campi di ordini e unità
        {
            try
            {
                AccessToken token;

                Snapshot? snap = snapshotBuffer.GetLastSnapshot(uniqueID);

                if (IsSnapshotReferenced(snap))
                {
                    // Resource is locked -> return read-only snapshot
                    token = new AccessToken(handler.ID, AccessMode.ReadOnly, snap);
                }
                else
                {
                    object? liveResource = await mapper.GetValueAsync(uniqueID);

                    //lock resource
                    LockResource(uniqueID, liveResource);

                    // Pass-through -> return pointer to live resource
                    token = new AccessToken(handler.ID, AccessMode.ReadWrite, liveResource); //probabile eccezzione non gestita (QUASI CERTA) 
                }

                tokens.Add(token);
                return token;
            }
            catch(Exception ex)
            {
                logger.LogError($"[{handler.ID}] Concurrency Manager Error: {ex} ");
                throw;
            }

        }

        public void ReleaseResource(AccessToken token)
        {
            if (!tokens.Contains(token))
                throw new InvalidOperationException("Unrecognizable Token.");
            tokens.Remove(token);
            if (token.Resource == iem)
                IEMSemaphore.Release();
        }


    }
}
