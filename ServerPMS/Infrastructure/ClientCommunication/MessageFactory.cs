
using System.Text;
using ServerPMS.Abstractions.Infrastructure.ClientCommunication;
namespace ServerPMS.Infrastructure.ClientCommunication
{
	public class MessageFactory : IMessageFactory
	{
        public string GetString(Message msg)
        {
            string decodedPayload = "";
            if (msg.Length > 0)
            {
                decodedPayload = Encoding.UTF8.GetString(msg.Payload);
            }

            return string.Format("CID: {0} ID: {1} Payload: {2}", ((CID)msg.CID).ToString(), new Guid(msg.ID).ToString(), decodedPayload);
        }


        public byte[] Serialize(Message message)
        {
            uint totalLength = (uint)(22 + (message.Payload?.Length ?? 0));
            byte[] buffer = new byte[totalLength];

            BitConverter.GetBytes(totalLength).CopyTo(buffer, 0);

            // Make sure message.ID is exactly 16 bytes
            if (message.ID.Length != 16)
                throw new ArgumentException("Message ID must be 16 bytes.");

            message.ID.CopyTo(buffer, 4);

            buffer[20] = message.CID;
            buffer[21] = message.Flags;

            message.Payload?.CopyTo(buffer, 22);

            return buffer;
        }

        public Message Deserialize(byte[] buffer)
        {
            if (buffer.Length < 22)
                throw new ArgumentException("Invalid buffer length");

            uint length = BitConverter.ToUInt32(buffer, 0);
            byte[] IdBytes = new byte[16];
            Array.Copy(buffer, 4, IdBytes, 0, IdBytes.Length);
            byte cid = buffer[20];
            byte flags = buffer[21];
            byte[] payload = new byte[length - 22];
            Array.Copy(buffer, 22, payload, 0, payload.Length);

            return new Message
            {
                Length = (uint)payload.Length,
                ID =  IdBytes,
                CID = cid,
                Flags = flags,
                Payload = payload
            };
        }

        public Message NewMessage(byte cid, byte[] id, byte flags, string payload)
        {
            byte[] _payload = Encoding.UTF8.GetBytes(payload);
            return new Message
            {
                CID = cid,
                ID = id,
                Flags = flags,
                Payload =_payload,
                Length = (uint)_payload.Length
            };
        }

        public Message NewMessage(byte cid, byte flags, string payload)
        {
            return NewMessage(cid,Guid.NewGuid().ToByteArray(),flags, payload);
        }

        public Message NewMessage(CID cid, Guid id,Flags flags, string payload)
        {
            byte[] _payload = Encoding.UTF8.GetBytes(payload);
            return new Message
            {
                CID = (byte)cid,
                ID=id.ToByteArray(),
                Flags = (byte)flags,
                Payload = _payload,
                Length = (uint)_payload.Length
               
            };
        }

        public Message NewMessage(CID cid, Flags flags, string payload)
        {
            return NewMessage(cid, Guid.NewGuid(), flags, payload);
        }

        public Message AckMessage(byte[] targetID)
        {

            return new Message
            {
                CID = (byte)CID.AKC,
                ID = Guid.NewGuid().ToByteArray(),
                Flags = (byte)Flags.None,
                Payload = targetID,
                Length = 16

            };
        }

        public Message NackMessage()
        {
            return new Message
            {
                CID = (byte)CID.NACK,
                ID = Guid.NewGuid().ToByteArray(),
                Flags = (byte)Flags.None,
                Length = 0
            };
        }

        public Message Heartbeat()
        {
            return new Message
            {
                CID = (byte)CID.HEARTBEAT,
                ID = Guid.NewGuid().ToByteArray(),
                Flags = (byte)Flags.None,
                Length = 0
            };
        }

        public Message Logout()
        {
            return new Message
            {
                CID = (byte)CID.LOGOUT,
                ID = Guid.NewGuid().ToByteArray(),
                Flags = (byte)Flags.None,
                Length = 0
            };
        }
    }
}

