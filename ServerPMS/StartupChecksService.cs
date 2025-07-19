using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ServerPMS.Abstractions.Infrastructure.Config;
using ServerPMS.Infrastructure.Config;

namespace ServerPMS
{
    public class StartupChecksService
	{
        private static readonly string BASE_DIRECTORY = "./data";
        private static readonly string CCENV_FILE_NAME = BASE_DIRECTORY + "/_cc.env";
        private static readonly string DB_FILE_NAME = BASE_DIRECTORY + "/test2.db";
        private static readonly string ENCRYPTED_CONFIG_FILE_NAME = BASE_DIRECTORY + "/pmsconf.enc";
        private static readonly string WAL_FILE_NAME = BASE_DIRECTORY + "/wal.walfile";
        private static readonly string DB_GEN_SQL = "PRAGMA foreign_keys = off;BEGIN TRANSACTION;CREATE TABLE IF NOT EXISTS prod_orders (ID INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, part_code TEXT NOT NULL, part_desc TEXT NOT NULL, qty INTEGER NOT NULL, customer_ord_ref TEXT, default_prod_unit INTEGER, mold_id TEXT NOT NULL, mold_location TEXT NOT NULL, mold_notes TEXT NOT NULL, customer_name TEXT NOT NULL, delivery_facility TEXT NOT NULL, delivery_date TEXT NOT NULL, status INTEGER NOT NULL);CREATE TABLE IF NOT EXISTS prod_units (ID INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, name TEXT NOT NULL UNIQUE, type INTEGER NOT NULL, status INTEGER NOT NULL, notes TEXT, current_production_order INTEGER, current_queue INTEGER);CREATE TABLE IF NOT EXISTS settings (ID INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, key TEXT NOT NULL, value TEXT NOT NULL );CREATE TABLE IF NOT EXISTS units_queues (ID INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, unit_id INTEGER NOT NULL REFERENCES prod_units (ID), order_id INTEGER NOT NULL UNIQUE REFERENCES prod_orders (ID), position INTEGER NOT NULL);CREATE TABLE IF NOT EXISTS users (ID INTEGER PRIMARY KEY NOT NULL, username TEXT UNIQUE NOT NULL, psw_hash TEXT UNIQUE NOT NULL, last_access INTEGER NOT NULL);COMMIT TRANSACTION;PRAGMA foreign_keys = on;";

        private readonly IGlobalConfigManager GlobalConfig;
        private readonly IConfigCrypto ConfigCrypto;

        private class PMSDefaultConfig : PMSConfig
        {
            public PMSDefaultConfig()
            {
                base.SoftwareDescTable = new SDT { Author = "LS Data", Version = "2.0 Dev", PackageName = "PMS" };
                base.Database = new SQLiteDatabaseConf { FilePath = DB_FILE_NAME, Timeout = 30 };
                base.WAL = new WALConf { WALFilePath = WAL_FILE_NAME };
                base.UnitsIDs = null;
                base.Users = new Personnel { users = null };
            }
        }

        public StartupChecksService(IConfigCrypto configCrypto, IGlobalConfigManager globalConfigManager) {

            GlobalConfig = globalConfigManager;
            ConfigCrypto = configCrypto; 
        }

        public Task RunChecksAsync()
        {
            //check if proper directory structure exists if not creates it
            CheckBaseDirectoryStructure();

            //check if configuration cryptograhy env (cc.env) file exits if not creates it
            CheckLoadCryptoConfEnvFile();

            //check if pms_database (pms_database.db) file exits if not creates it 
            CheckDBFile();

            //check if pms configuration (pmsconf.enc) file exits if not creates it 
            CheckLoadPMSConfigurationFile();

            IsInternetAvailable();

            return Task.CompletedTask;
        }

        public void RunChecks()
        {
            RunChecksAsync().Wait();
        }

        private bool CheckBaseDirectoryStructure()
        {
            bool wasPresent = true;

            //check if main data directory exists
            if (!Directory.Exists(BASE_DIRECTORY))
            {
                wasPresent = false;

                //create main data directory and backup subdirectory
                Directory.CreateDirectory(BASE_DIRECTORY);
                Directory.CreateDirectory(BASE_DIRECTORY + "/Backups");
            }
            else
            {
                //check if backup subdirectory exists
                if (!Directory.Exists(BASE_DIRECTORY + "/Backups"))
                {
                    wasPresent = false;

                    //create backup subdirectory
                    Directory.CreateDirectory(BASE_DIRECTORY + "/Backups");
                }
            }
            return wasPresent;
        }

        private bool CheckDBFile()
        {
            bool wasPresentDB = true;

            //check if file exists
            if (!File.Exists(DB_FILE_NAME))
            {
                wasPresentDB = false;

                //create file
                File.Create(DB_FILE_NAME);

                SqliteConnectionStringBuilder builder = new();
                builder.DefaultTimeout = GlobalConfig.GlobalRAMConfig.Database.Timeout;
                builder.Cache = SqliteCacheMode.Default;
                builder.DataSource = DB_FILE_NAME;

                using (SqliteConnection c = new SqliteConnection(builder.ConnectionString))
                {
                    c.Open();
                    SqliteCommand cmd = c.CreateCommand();
                    cmd.CommandText = DB_GEN_SQL;

                    //generate the DB
                    cmd.ExecuteNonQuery();
                }
            }

            //check if application-level WAL file exists
            if (!File.Exists(WAL_FILE_NAME))
            {
                File.WriteAllText(WAL_FILE_NAME, string.Empty);
            }
            return wasPresentDB;
        }

        private bool CheckLoadPMSConfigurationFile()
        {
            bool wasPresent = true;

            //check if file exists
            if (!File.Exists(ENCRYPTED_CONFIG_FILE_NAME))
            {
                wasPresent = false;

                //serialize a default configuration
                string json = JsonSerializer.Serialize(new PMSDefaultConfig(), new JsonSerializerOptions { WriteIndented = true });

                //create and write to encrypted file
                ConfigCrypto.EncryptToFile(json, ENCRYPTED_CONFIG_FILE_NAME);
            }
            GlobalConfig.EncryptedConfigPath = ENCRYPTED_CONFIG_FILE_NAME;
            GlobalConfig.KeyFilePath = CCENV_FILE_NAME;
            GlobalConfig.Load();
            return wasPresent;
        }

        private bool CheckLoadCryptoConfEnvFile()
        {
            bool wasPresent = true;

            //check if file exists
            if (!File.Exists(CCENV_FILE_NAME))
            {

                wasPresent = false;

                //generate keys
                byte[] key = RandomNumberGenerator.GetBytes(32); // AES-256
                byte[] iv = RandomNumberGenerator.GetBytes(16);  // AES block size

                using (FileStream fs = new FileStream(CCENV_FILE_NAME, FileMode.CreateNew, FileAccess.Write))
                {
                    using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                    {
                        //write keys
                        sw.WriteLine(Convert.ToBase64String(key));
                        sw.WriteLine(Convert.ToBase64String(iv));
                    }
                }

                //remove old permanenly encrypted config file
                if (File.Exists(ENCRYPTED_CONFIG_FILE_NAME))
                    File.Delete(ENCRYPTED_CONFIG_FILE_NAME);
            }
            ConfigCrypto.EnvFilePath = CCENV_FILE_NAME;
            return wasPresent;
        }

        private bool IsInternetAvailable()
        {
            try
            {
                using HttpClient client = new();
                client.Timeout = TimeSpan.FromSeconds(3);
                using HttpResponseMessage response = client.GetAsync("https://www.industrieresinestampate.it").Result;
                return response.IsSuccessStatusCode;
            }
            catch
            {
                throw new InvalidOperationException("Internet unreachable. Check Internet connection and retry.");
            }
        }
    }
}

