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
            string filename = "/Users/edoardosanti/Downloads/TEST_IRS.xlsx";

            PMSCore core = new PMSCore();

            core.ImportOrderExcel(filename);
            Console.WriteLine(core.StrDumpBuffer());
            
        }
    }
} 