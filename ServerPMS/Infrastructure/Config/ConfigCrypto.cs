// PMS Project V1.0
// LSData - all rights reserved
// ConfigurationCrypto.cs
//
using System;
using System.Text;
using System.Security.Cryptography;
using ServerPMS.Abstractions.Infrastructure.Config;

namespace ServerPMS.Infrastructure.Config
{

    public class ConfigCrypto: IConfigCrypto
    {
        public string EnvFilePath { get; set; }

        private byte[] Key;
        private byte[] IV;

        private bool isKeyParsed = false;

        private void ParseKey(string envPath)
        {
            if (!isKeyParsed)
            {
                string[] lines = File.ReadAllLines(envPath);
                Key = Convert.FromBase64String(lines[0]);
                IV = Convert.FromBase64String(lines[1]);
                isKeyParsed = true;
            }
        }

        public void EncryptToFile(string msg, string outputPath)
        {
            ParseKey(EnvFilePath);
            var plainBytes = Encoding.UTF8.GetBytes(msg);
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;

            using var encryptor = aes.CreateEncryptor();
            var cipher = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            
            File.WriteAllBytes(outputPath, cipher);
        }

        public string DecryptFromFile(string inputPath)
        {
            ParseKey(EnvFilePath);
            var cipherBytes = File.ReadAllBytes(inputPath);
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;

            using var decryptor = aes.CreateDecryptor();
            var plain = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plain);
        }
    }

}

