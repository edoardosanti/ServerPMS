using System;
namespace ServerPMS.Infrastructure.ClientCommunication
{
    public record struct Message
    {
        public uint Length;      // 4 bytes
        public byte[] ID;        // 16-byte
        public byte CID;         // 1 byte
        public byte Flags;       // 1 byte
        public byte[] Payload;   // Variable
    }
}

