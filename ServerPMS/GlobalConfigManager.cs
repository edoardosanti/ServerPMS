// PMS Project V1.0
// LSData - all rights reserved
// PMSConfigLoader.cs
//
//
using System;
using System.Reflection.Emit;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace ServerPMS
{
    static public class GlobalConfigManager
    {
        public static PMSConfig GlobalConfig { private set; get; }
        public static PMSConfig RAMConfig { set; get; }
        
        public static void Load(string encryptedConfigPath, string keyFilePath)
        {
            // Set the encryption key file path for ConfigCrypto
            ConfigCrypto.EnvFilePath = keyFilePath;

            // Decrypt the configuration file into a JSON string
            string json = ConfigCrypto.DecryptFromFile(encryptedConfigPath);

            // Deserialize the JSON into the strongly typed config class
            GlobalConfig = JsonSerializer.Deserialize<PMSConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            //RAMConfig = GlobalConfig TODO: ICloenable su tutte le classi utili.

            if (GlobalConfig == null)
                throw new InvalidOperationException("Failed to load decrypted configuration.");


        }
    }
}

