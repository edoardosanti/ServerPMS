using System;
using System.Text;
using ServerPMS.Abstractions.Infrastructure.ClientCommunication;
namespace ServerPMS.Infrastructure.ClientCommunication
{
	public class MessageFactory : IMessageFactory
	{
        public byte[] GetBytes(Message message)
        {
            message.Length = (uint)(6 + (message.Payload?.Length ?? 0));
            byte[] buffer = new byte[message.Length];
            BitConverter.GetBytes(message.Length).CopyTo(buffer, 0);
            buffer[4] = message.CID;
            buffer[5] = message.Flags;
            if (message.Payload != null)
                message.Payload.CopyTo(buffer, 6);
            return buffer;
        }

        public Message GetMessage(byte[] buffer)
        {
            if (buffer.Length < 6)
                throw new ArgumentException("Invalid buffer length");

            var length = BitConverter.ToUInt32(buffer, 0);
            var cid = buffer[4];
            var flags = buffer[5];
            var payload = new byte[length - 6];
            Array.Copy(buffer, 6, payload, 0, payload.Length);

            return new Message
            {
                Length = length,
                CID = cid,
                Flags = flags,
                Payload = payload
            };
        }

        public Message GetMessage(byte cid, byte flags, string payload)
        {
            return new Message
            {
                CID = cid,
                Flags = flags,
                Payload = Encoding.UTF8.GetBytes(payload)
            };
        }

        public Message GetMessage(CID cid, Flags flags, string payload)
        {
            byte[] _payload = Encoding.UTF8.GetBytes(payload);
            return new Message
            {
                CID = (byte)cid,
                Flags = (byte)flags,
                Payload = _payload,
                Length = (uint)_payload.Length
               
            };
        }

        public Message AckMessage()
        {
            return new Message
            {
                CID = (byte)CID.AKC,
                Flags = (byte)Flags.None,
                Length = 0
            };
        }

        public Message NackMessage()
        {
            return new Message
            {
                CID = (byte)CID.NACK,
                Flags = (byte)Flags.None,
                Length = 0
            };
        }

        public Message Heartbeat()
        {
            return new Message
            {
                CID = (byte)CID.HEARTBEAT,
                Flags = (byte)Flags.None,
                Length = 0
            };
        }
    }
}

