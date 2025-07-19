// PMS Project V1.0
// LSData - all rights reserved
// WALLogger.cs
//
//

using System.Text;
using ServerPMS.Abstractions.Infrastructure.Config;
using ServerPMS.Abstractions.Infrastructure.Logging;

namespace ServerPMS.Infrastructure.Logging
{
    public class WALLogger : IWALLogger, IDisposable
    {
        private BinaryWriter _writer;
        private FileStream _stream;
        public string WALFilePath { get; set; }

        private readonly IGlobalConfigManager GlobalConfig;


        public WALLogger(IGlobalConfigManager globalConfig)
        {
            GlobalConfig = globalConfig;
            WALFilePath = GlobalConfig.GlobalRAMConfig.WAL.WALFilePath;
            _stream = new FileStream(WALFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            _writer = new BinaryWriter(_stream);
        }

        public void Log(string sql)
        {
            var bytes = Encoding.UTF8.GetBytes(sql);
            _writer.Write(bytes.Length);
            _writer.Write(bytes);
            _writer.Flush(); // ensure it’s on disk
        }

        public IEnumerable<string> Replay()
        {
            if (!File.Exists(WALFilePath)) yield break;

            using var stream = new FileStream(WALFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(stream);

            while (stream.Position < stream.Length)
            {
                int len;
                byte[] bytes;
                try
                {
                    len = reader.ReadInt32();
                    bytes = reader.ReadBytes(len);
                   
                }
                catch
                {
                    yield break; 
                }
                yield return Encoding.UTF8.GetString(bytes);
                
            }
        }

        public void Flush()
        {
            _writer.Flush();
            _stream.SetLength(0);
        }

        public void Dispose()
        {
            _writer?.Dispose();
            _stream?.Dispose();
        }
    }

}

