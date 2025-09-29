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
        private readonly IUnitsManager unitsManager;
        private readonly IQueuesManager queuesManager;
        private readonly IGlobalConfigManager configManager;
        private readonly ISnapshotBuffer snapshotBuffer;
        private readonly IIntegratedEventsManager iem;
        private readonly IResourceMapper mapper;

        private HashSet<AccessToken> tokens;
        public List<AccessToken> ActiveTokens => tokens.ToList<AccessToken>();

        private SemaphoreSlim IEMSemaphore;
        private SemaphoreSlim OMSemaphore;
        private SemaphoreSlim UMSemaphore;
        private SemaphoreSlim QMSemaphore;

        public ConcurrencyManager(ILogger<ConcurrencyManager> logger, IGlobalConfigManager configManager, ISnapshotBuffer snapshotBuffer, IAppCore core, IResourceMapper resourceMapper)
        {
            iem = core.IEM;
            ordersManager = core.OrdersManager;
            unitsManager = core.UnitsManger;
            queuesManager = core.QueuesManager;
            this.logger = logger;
            this.configManager = configManager;
            this.snapshotBuffer = snapshotBuffer;
            mapper = resourceMapper;
            tokens = new HashSet<AccessToken>();
            IEMSemaphore = new SemaphoreSlim(1);
            OMSemaphore = new SemaphoreSlim(1);
            UMSemaphore = new SemaphoreSlim(1);
            QMSemaphore = new SemaphoreSlim(1);

        }

        private void LockResource(string uniqueID,object value)
        {
           snapshotBuffer.NewSnapshot(uniqueID,value);
        }

        public async Task<AccessToken> AccessIEMAsync(ClientHandler handler)
        {
            return await AccessIEMAsync(handler.ID);
        }

        public async Task<AccessToken> AccessIEMAsync(string handlerID)
        {
            //wait for iem
            await IEMSemaphore.WaitAsync();

            //prepare token
            AccessToken at = new AccessToken(handlerID, AccessMode.ReadWrite, "IEM", iem);
            tokens.Add(at);

            return at;
        }

        public async Task<AccessToken> AccessOrdersManagerAsync(ClientHandler handler)
        {
            return await AccessOrdersManagerAsync(handler.ID);
        }

        public async Task<AccessToken> AccessOrdersManagerAsync(string handlerID)
        {
            //wait for iem
            await OMSemaphore.WaitAsync();

            //prepare token
            AccessToken at = new AccessToken(handlerID, AccessMode.ReadWrite, "OM", ordersManager);
            tokens.Add(at);

            return at;
        }

        public async Task<AccessToken> AccessUnitsManagerAsync(ClientHandler handler)
        {
            return await AccessUnitsManagerAsync(handler.ID);
        }

        public async Task<AccessToken> AccessUnitsManagerAsync(string handlerID)
        {
            //wait for iem
            await UMSemaphore.WaitAsync();

            //prepare token
            AccessToken at = new AccessToken(handlerID, AccessMode.ReadWrite, "UM", unitsManager);
            tokens.Add(at);

            return at;
        }

        public async Task<AccessToken> AccessQueuesManagerAsync(ClientHandler handler)
        {
            return await AccessQueuesManagerAsync(handler.ID);
        }

        public async Task<AccessToken> AccessQueuesManagerAsync(string handlerID)
        {
            //wait for iem
            await QMSemaphore.WaitAsync();

            //prepare token
            AccessToken at = new AccessToken(handlerID, AccessMode.ReadWrite, "QM", queuesManager);
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
                    token = new AccessToken(handler.ID, AccessMode.ReadOnly, uniqueID, snap);
                }
                else
                {
                    object? liveResource = await mapper.GetValueAsync(uniqueID);

                    //lock resource
                    LockResource(uniqueID, liveResource);

                    // Pass-through -> return pointer to live resource
                    token = new AccessToken(handler.ID, AccessMode.ReadWrite, uniqueID, liveResource); //probabile eccezzione non gestita (QUASI CERTA) 
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
            snapshotBuffer.ClearSnapshots(token.ResourceID);
            if (token.ResourceID == "IEM")
                IEMSemaphore.Release();
            if (token.ResourceID == "OM")
                OMSemaphore.Release();
        }


    }
}
