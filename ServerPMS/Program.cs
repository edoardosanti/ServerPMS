using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace ServerPMS
{

    class Program
    {
        private static readonly string CCENV_FILE_NAME = "_cc.env";
        private static readonly string DB_FILE_NAME = "pms_database.db";
        private static readonly string ENCRYPTED_CONFIG_FILE_NAME = "pmsconf.enc";

        private static readonly string DB_GEN_SQL = "PRAGMA foreign_keys = off;BEGIN TRANSACTION;CREATE TABLE IF NOT EXISTS prod_orders (ID INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, ID_pms INTEGER NOT NULL UNIQUE, part_code TEXT NOT NULL, part_desc TEXT NOT NULL, qty INTEGER NOT NULL, customer_ord_ref TEXT, default_prod_unit INTEGER, mold_id TEXT NOT NULL, mold_location TEXT NOT NULL, mold_notes TEXT NOT NULL, customer_name TEXT NOT NULL, delivery_facility TEXT NOT NULL, delivery_date TEXT NOT NULL, order_status TEXT NOT NULL);CREATE TABLE IF NOT EXISTS prod_units (ID INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, name TEXT NOT NULL UNIQUE, type TEXT NOT NULL, status TEXT NOT NULL, notes TEXT, current_production_order INTEGER);CREATE TABLE IF NOT EXISTS settings (ID INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, key TEXT NOT NULL, value TEXT NOT NULL);CREATE TABLE IF NOT EXISTS units_queues (ID INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, unit_id INTEGER NOT NULL, prod_order_id INTEGER NOT NULL, queue_pos INTEGER NOT NULL);COMMIT TRANSACTION;PRAGMA foreign_keys = on;";
        
        static void Main(string[] args)
        {

            //check if configuration cryptograhy env (cc.env) file exits if not create it
            CheckLoadCryptoConfEnvFile();

            //check if pms_database (pms_database.db) file exits if not create it 
            CheckDBFile();

            //check if pms configuration (pmsconf.enc) file exits if not create it 
            CheckLoadPMSConfigurationFile();

            string filename = "/Users/edoardosanti/Downloads/TEST_IRS_2.xlsx";
            string dbTest = "test.db"; 

            PMSCore core = new PMSCore(new PMSCoreSettings() { SQLITE_DB_FILE = dbTest, WAL_PATH = "wal.walfile"});

            core.ImportOrdersFromExcelFile(filename);
            Console.WriteLine(core.StrDumpBuffer());
            
        }

        static bool CheckDBFile()
        {
            bool wasPresent = true;

            //check if file exists
            if (!File.Exists(DB_FILE_NAME))
            {
                wasPresent = false;
                //create file
                File.Create(DB_FILE_NAME);
                using (SqliteConnection c = new SqliteConnection(string.Format("Data Source={0};Mode=ReadWrite;", DB_FILE_NAME)))
                {
                    //generate tables
                    new SqliteCommand(DB_GEN_SQL).ExecuteNonQuery();
                }

            }
            return wasPresent;
        }

        static bool CheckLoadPMSConfigurationFile()
        {
            bool wasPresent=true;

            //check if file exists
            if (!File.Exists(ENCRYPTED_CONFIG_FILE_NAME))
            {
                wasPresent = false;

                //create file
                File.Create(ENCRYPTED_CONFIG_FILE_NAME);

                //serialize a default configuration
                string json = JsonSerializer.Serialize(new PMSDefaultConfig(), new JsonSerializerOptions { WriteIndented = true });

                //write to encrypted file
                ConfigCrypto.EncryptToFile(json, ENCRYPTED_CONFIG_FILE_NAME);
            }
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
            }
            ConfigCrypto.EnvFilePath = CCENV_FILE_NAME;
            return wasPresent;
        }
    }
} 