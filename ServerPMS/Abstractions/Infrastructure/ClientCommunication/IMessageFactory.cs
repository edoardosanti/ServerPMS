using System;
using ServerPMS.Infrastructure.ClientCommunication;

namespace ServerPMS.Abstractions.Infrastructure.ClientCommunication
{
	public interface IMessageFactory
	{

        //methods
        public byte[] GetBytes(Message message);
        public Message GetMessage(byte[] bytes);
        public Message GetMessage(byte cid, byte flags, string payload);
        public Message GetMessage(CID cid, Flags flags, string payload);
        public Message AckMessage();
        public Message NackMessage();
        public Message Heartbeat();

    }
}

