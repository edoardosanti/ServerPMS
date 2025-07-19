// PMS Project V1.0
// LSData - all rights reserved
// PMSConfigLoader.cs
//
//

using System.Text.Json;
using ServerPMS.Abstractions.Infrastructure.Config;

namespace ServerPMS.Infrastructure.Config
{
    public class GlobalConfigManager : IGlobalConfigManager
    {
        public PMSConfig GlobalFileConfig { get; private set; }
        public PMSConfig GlobalRAMConfig { set; get; }

        public string EncryptedConfigPath { get; set; }
        public string KeyFilePath { get; set; }

        private readonly IConfigCrypto _configCrypto;

        public GlobalConfigManager(IConfigCrypto configCrypto)
        {
            _configCrypto = configCrypto;
            GlobalFileConfig = null;
            GlobalRAMConfig = null;
            EncryptedConfigPath = string.Empty;
            KeyFilePath = string.Empty;
        }

        public void Load()
        {
            // Set the encryption key file path for ConfigCrypto

            if (EncryptedConfigPath != string.Empty && KeyFilePath != string.Empty)
            {

                // Decrypt the configuration file into a JSON string
                string json = _configCrypto.DecryptFromFile(EncryptedConfigPath);
                // Deserialize the JSON into the strongly typed config class
                GlobalFileConfig = JsonSerializer.Deserialize<PMSConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                //load config to ram
                GlobalRAMConfig = GlobalFileConfig.Clone();

                if (GlobalFileConfig == null)
                    throw new InvalidOperationException("Failed to load decrypted configuration.");

            }
        }

        public void DumpRAMConfigToFile()
        {
            //serialize ram configuration
            string json = JsonSerializer.Serialize(GlobalRAMConfig, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            //write to encrypted file
            _configCrypto.EncryptToFile(json, EncryptedConfigPath);

            //reload file
            Load();
            
        }
    }
}

