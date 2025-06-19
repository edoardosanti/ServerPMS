using System.Data;
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

            string filename = "/Users/edoardosanti/Downloads/TEST_IRS.xlsx";

            PMSCore core = new PMSCore();

            core.ImportOrdersFromExcelFile(filename);
            Console.WriteLine(core.StrDumpBuffer());
            
        }
    }
} 