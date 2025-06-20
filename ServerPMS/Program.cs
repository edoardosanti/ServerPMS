using System.Data;
using Microsoft.Data.Sqlite;
using System.Globalization;
using System.Reflection;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;

namespace ServerPMS
{

    class Program
    {
        static void Main(string[] args)
        {
            //debug 

            SDT SDT = new SDT("LS Data", "2.0", "PMS");

            string filename = "/Users/edoardosanti/Downloads/TEST_IRS_2.xlsx";
            string dbTest = "test.db";

             

            PMSCore core = new PMSCore(new PMSCoreSettings() { SQLITE_DB_FILE = dbTest, WAL_PATH = "wal.walfile"});


            core.CmdDBA.EnqueueSql("INSERT INTO settings (key, value) VALUES ('LS','Media'),('LS','Data');");

            core.ImportOrdersFromExcelFile(filename);
            Console.WriteLine(core.StrDumpBuffer());
            
        }
    }
} 