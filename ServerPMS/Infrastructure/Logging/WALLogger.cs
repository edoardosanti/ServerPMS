// PMS Project V1.0
// LSData - all rights reserved
// WALLogger.cs
//
//

using System.Text;

namespace ServerPMS
{
    public static class WALLogger
    {
        private static BinaryWriter _writer;
        private static FileStream _stream;
        public static string WALFilePath { get; set; }


        public static void Start()
        {
            _stream = new FileStream(WALFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            _writer = new BinaryWriter(_stream);
        }

        public static void Log(string sql)
        {
            var bytes = Encoding.UTF8.GetBytes(sql);
            _writer.Write(bytes.Length);
            _writer.Write(bytes);
            _writer.Flush(); // ensure it’s on disk
        }

        public static IEnumerable<string> Replay()
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

        public static void Flush()
        {
            _writer.Flush();
            _stream.SetLength(0);
        }

        public static void Dispose()
        {
            _writer?.Dispose();
            _stream?.Dispose();
        }
    }

}

