using ServerPMS.Abstractions.Infrastructure.ClientCommunication;
using System.Net.Security;


namespace ServerPMS.Infrastructure.ClientCommunication
{
    public class StreamParser : IStreamParser
    {

        private readonly SslStream stream;
        public StreamParser(SslStream stream)
        {
            this.stream = stream;
        }


        private async Task ReadExactlyAsync(Stream stream, byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead);
                if (read == 0)
                    throw new IOException("Unexpected end of stream");
                totalRead += read;
            }
        }

        public async Task<byte[]> GetMessageBytesAsync()
        {
            byte[] buffer = new byte[4096];
            // Step 1: Read exactly 4 bytes (Length prefix)
            await ReadExactlyAsync(stream, buffer, 0, 4);
            uint totalLength = BitConverter.ToUInt32(buffer, 0);

            // Step 2: Read the remaining part of the message
            await ReadExactlyAsync(stream, buffer, 4, (int)(totalLength - 4));

            // Step 3: Deserialize the whole message
            return buffer[..(int)totalLength];

        }
    }
}

