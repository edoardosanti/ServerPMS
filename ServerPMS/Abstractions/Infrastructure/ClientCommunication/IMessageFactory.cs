using System;
using ServerPMS.Infrastructure.ClientCommunication;

namespace ServerPMS.Abstractions.Infrastructure.ClientCommunication
{
	public interface IMessageFactory
	{

        //methods
        public string GetString(Message message);
        public byte[] Serialize(Message message);
        public Message Deserialize(byte[] bytes);
        public Message NewMessage(byte cid, byte flags, string payload);
        public Message NewMessage(byte cid, byte[] id, byte flags, string payload);
        public Message NewMessage(CID cid, Flags flags, string payload);
        public Message NewMessage(CID cid,Guid id, Flags flags, string payload);
        public Message AckMessage(byte[] targetId);
        public Message NackMessage();
        public Message Heartbeat();
        public Message Logout();

    }
}

