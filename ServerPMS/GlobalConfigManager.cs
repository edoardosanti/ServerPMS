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
using System.Runtime.CompilerServices;

namespace ServerPMS
{
    static public class GlobalConfigManager
    {
        public static PMSConfig GlobalFileConfig { private set; get; }
        public static PMSConfig GlobalRAMConfig { set; get; }

        public static string EncryptedConfigPath { get; set; }
        public static string KeyFilePath { get; set; }

        static GlobalConfigManager()
        {
            GlobalFileConfig = null;
            GlobalRAMConfig = null;
            EncryptedConfigPath = string.Empty;
            KeyFilePath = string.Empty;
        }

        public static void Load()
        {
            // Set the encryption key file path for ConfigCrypto

            if (EncryptedConfigPath != string.Empty && KeyFilePath != string.Empty)
            {

                // Decrypt the configuration file into a JSON string
                string json = ConfigCrypto.DecryptFromFile(EncryptedConfigPath);

                // Deserialize the JSON into the strongly typed config class
                GlobalFileConfig = JsonSerializer.Deserialize<PMSConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                //load config to ram
                GlobalRAMConfig = GlobalFileConfig.Clone();

                if (GlobalFileConfig == null)
                    throw new InvalidOperationException("Failed to load decrypted configuration.");

            }
        }

        public static void DumpRAMConfigToFile()
        {
            //serialize ram configuration
            string json = JsonSerializer.Serialize(GlobalRAMConfig, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            //write to encrypted file
            ConfigCrypto.EncryptToFile(json, EncryptedConfigPath);

            //reload file
            Load();
            
        }
    }
}

