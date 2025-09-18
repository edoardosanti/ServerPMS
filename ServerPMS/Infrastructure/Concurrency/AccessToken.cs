using System;

namespace ServerPMS.Infrastructure.Concurrency
{
    public class AccessToken
    {
        public string TokenID;
        public string ClientID;
        public AccessMode AccessMode;
        public object? Resource;
        public Snapshot? Snapshot = default;

        public bool IsPassThrough => Resource != null;

        public AccessToken(string clientID, AccessMode mode, object resource)
        {
            TokenID = Guid.NewGuid().ToString();
            ClientID = clientID;
            AccessMode = mode;
            Resource = resource;
        }

        public AccessToken(string clientID, AccessMode mode, Snapshot snapshot)
        {
            TokenID = Guid.NewGuid().ToString();
            ClientID = clientID;
            AccessMode = mode;
            Snapshot = snapshot;
        }

    }
}

