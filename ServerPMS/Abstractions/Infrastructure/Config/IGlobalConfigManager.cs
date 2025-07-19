using System;
using ServerPMS.Infrastructure.Config;

namespace ServerPMS.Abstractions.Infrastructure.Config
{
	public interface IGlobalConfigManager
	{
        //properties
        PMSConfig GlobalFileConfig { get; }
        PMSConfig GlobalRAMConfig { set; get; }

        string EncryptedConfigPath { get; set; }
        string KeyFilePath { get; set; }

        //methods
        void Load();
        void DumpRAMConfigToFile();

    }
}

