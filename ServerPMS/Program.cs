using System.Data;
using Microsoft.Data.Sqlite;
using System.Globalization;
using System.Reflection;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System.Dynamic;
using System.Security.Cryptography;
using System.Text;

namespace ServerPMS
{

    class Program
    {
        static void Main(string[] args)
        {
            //debug 

            SDT SDT = new SDT("LS Data", "2.0", "PMS");

            //check if cryptoconf file exits if 

            if (!File.Exists("cc.env"))
            {

                byte[] key = RandomNumberGenerator.GetBytes(32); // AES-256
                byte[] iv = RandomNumberGenerator.GetBytes(16);  // AES block size

                using (FileStream fs = new FileStream("cc.env", FileMode.CreateNew, FileAccess.Write))
                {
                    using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                    {
                        sw.WriteLine(Convert.ToBase64String(key));
                        sw.WriteLine(Convert.ToBase64String(iv));

                    }
                }

            }



            string filename = "/Users/edoardosanti/Downloads/TEST_IRS_2.xlsx";
            string dbTest = "test.db"; 

            PMSCore core = new PMSCore(new PMSCoreSettings() { SQLITE_DB_FILE = dbTest, WAL_PATH = "wal.walfile"});

            core.ImportOrdersFromExcelFile(filename);
            Console.WriteLine(core.StrDumpBuffer());
            
        }
    }
} 