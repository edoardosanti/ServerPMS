// PMS Project V1.0
// LSData - all rights reserved
// WALLogger.cs
//
//
using System;
using System.Text;
using System.Text.Json;
using DocumentFormat.OpenXml.ExtendedProperties;

namespace ServerPMS
{
    public class WALLogger : IDisposable
    {
        private readonly BinaryWriter _writer;
        private readonly FileStream _stream;
        public string WALFilePat { get; set; }

        public WALLogger(string path)
        {
            _stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            _writer = new BinaryWriter(_stream);
            WALFilePat = path;
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
            if (!File.Exists(WALFilePat)) yield break;

            using var stream = new FileStream(WALFilePat, FileMode.Open, FileAccess.Read, FileShare.Read);
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

