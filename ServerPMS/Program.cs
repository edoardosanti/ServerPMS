
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ServerPMS
{

    class Program
    {

        private static readonly string BASE_DIRECTORY = "./data";
        private static readonly string CCENV_FILE_NAME = BASE_DIRECTORY+ "/_cc.env";
        private static readonly string DB_FILE_NAME = BASE_DIRECTORY+ "/test2.db";
        private static readonly string ENCRYPTED_CONFIG_FILE_NAME = BASE_DIRECTORY+ "/pmsconf.enc";
        private static readonly string WAL_FILE_NAME = BASE_DIRECTORY+ "/wal.walfile";
        private static readonly string DB_GEN_SQL = "PRAGMA foreign_keys = off;BEGIN TRANSACTION;CREATE TABLE IF NOT EXISTS prod_orders (ID INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, part_code TEXT NOT NULL, part_desc TEXT NOT NULL, qty INTEGER NOT NULL, customer_ord_ref TEXT, default_prod_unit INTEGER, mold_id TEXT NOT NULL, mold_location TEXT NOT NULL, mold_notes TEXT NOT NULL, customer_name TEXT NOT NULL, delivery_facility TEXT NOT NULL, delivery_date TEXT NOT NULL, status INTEGER NOT NULL);CREATE TABLE IF NOT EXISTS prod_units (ID INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, name TEXT NOT NULL UNIQUE, type INTEGER NOT NULL, status INTEGER NOT NULL, notes TEXT, current_production_order INTEGER, current_queue INTEGER);CREATE TABLE IF NOT EXISTS settings (ID INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, key TEXT NOT NULL, value TEXT NOT NULL );CREATE TABLE IF NOT EXISTS units_queues (ID INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, unit_id INTEGER NOT NULL REFERENCES prod_units (ID), order_id INTEGER NOT NULL UNIQUE REFERENCES prod_orders (ID), position INTEGER NOT NULL);CREATE TABLE IF NOT EXISTS users (ID INTEGER PRIMARY KEY NOT NULL, username TEXT UNIQUE NOT NULL, psw_hash TEXT UNIQUE NOT NULL, last_access INTEGER NOT NULL);COMMIT TRANSACTION;PRAGMA foreign_keys = on;";

        public class PMSDefaultConfig : PMSConfig
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

        //loggers

        static void Main(string[] args)
        {

            //initialize loggers
            InitializeLoggers();

            //check if proper directory structure exists if not creates it
            CheckBaseDirectoryStructure();

            //check if configuration cryptograhy env (cc.env) file exits if not creates it
            CheckLoadCryptoConfEnvFile();

            //check if pms_database (pms_database.db) file exits if not creates it 
            CheckDBFile();

            //check if pms configuration (pmsconf.enc) file exits if not creates it 
            CheckLoadPMSConfigurationFile();

            //TODO: add check internet connection


            PMSCore core = new PMSCore();

            //core.ImportOrdersFromExcelFile("/Users/edoardosanti/Downloads/TEST_IRS_CHATGPT_DATE_FORMATTATE.xlsx");
            
        }
        static void InitializeLoggers()
        {
            // Set up Serilog for multi-target logging
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
                .WriteTo.File("logs/dbas.log", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information, outputTemplate: "[{Timestamp:HH:mm:ss} DBAs] {Message}{NewLine}")
                .WriteTo.File("logs/queues.log", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information, outputTemplate: "[{Timestamp:HH:mm:ss} QUEUES] {Message}{NewLine}")
                .WriteTo.File("logs/orders.log", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information, outputTemplate: "[{Timestamp:HH:mm:ss} ORDERS] {Message}{NewLine}")
                .WriteTo.File("logs/units.log", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information, outputTemplate: "[{Timestamp:HH:mm:ss} UNITS] {Message}{NewLine}")
                .WriteTo.File("logs/core.log", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information, outputTemplate: "[{Timestamp:HH:mm:ss} CORE] {Message}{NewLine}")
                .CreateLogger();
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(); // bridge Serilog to Microsoft.Extensions.Logging
            });

            Loggers.Init(loggerFactory);

        }
        static bool CheckBaseDirectoryStructure()
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
        static bool CheckDBFile()
        {
            bool wasPresentDB = true;

            //check if file exists
            if (!File.Exists(DB_FILE_NAME))
            {
                wasPresentDB = false;

                //create file
                File.Create(DB_FILE_NAME);

                SqliteConnectionStringBuilder builder = new();
                builder.DefaultTimeout = GlobalConfigManager.GlobalRAMConfig.Database.Timeout;
                builder.Cache = SqliteCacheMode.Default;
                builder.DataSource = DB_FILE_NAME;
               
                using (SqliteConnection c = new SqliteConnection(builder.ConnectionString))
                {
                    //TODO Check connection opening

                    //generate table
                    new SqliteCommand(DB_GEN_SQL).ExecuteNonQuery();
                }
            }

            //check if application-level WAL file exists
            if (!File.Exists(WAL_FILE_NAME))
            {
                File.WriteAllText(WAL_FILE_NAME, string.Empty);
            }
            return wasPresentDB;
        }
        static bool CheckLoadPMSConfigurationFile()
        {
            bool wasPresent=true;

            //check if file exists
            if (!File.Exists(ENCRYPTED_CONFIG_FILE_NAME))
            {
                wasPresent = false;

                //serialize a default configuration
                string json = JsonSerializer.Serialize(new PMSDefaultConfig(), new JsonSerializerOptions { WriteIndented = true }) ;

                //create and write to encrypted file
                ConfigCrypto.EncryptToFile(json, ENCRYPTED_CONFIG_FILE_NAME);
            }
            GlobalConfigManager.EncryptedConfigPath = ENCRYPTED_CONFIG_FILE_NAME;
            GlobalConfigManager.KeyFilePath = CCENV_FILE_NAME;
            GlobalConfigManager.Load();
            return wasPresent;
        }
        static bool CheckLoadCryptoConfEnvFile()
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
    }
} 