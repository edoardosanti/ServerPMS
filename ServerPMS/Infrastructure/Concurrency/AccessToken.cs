using System;

namespace ServerPMS.Infrastructure.Concurrency
{
    public class AccessToken
    {
        public string TokenID;
        public string ClientID;
        public string ResourceID;
        public AccessMode AccessMode;
        public object? Resource;
        public Snapshot? Snapshot = default;

        public bool IsPassThrough => Resource != null;

        public AccessToken(string clientID, AccessMode mode, string resID, object liveRes)
        {
            TokenID = Guid.NewGuid().ToString();
            ClientID = clientID;
            AccessMode = mode;
            Resource = liveRes;
            ResourceID = resID;
        }

        public AccessToken(string clientID, AccessMode mode, string resID, Snapshot snapshot)
        {
            TokenID = Guid.NewGuid().ToString();
            ClientID = clientID;
            AccessMode = mode;
            Snapshot = snapshot;
            ResourceID = resID;
        }

    }
}

