using System;

namespace PMSConfigurationUtilities;
class Program
{
    static void Main(string[] args)
    {
        //debug
        Console.Title = "PMS Configuration";
        Console.WriteLine("PMS Configuration Utilities\n\n");


        //select operation type
        Console.WriteLine("Select operation type: \n");
        Console.WriteLine("1) Local configuration (server)");
        //Console.WriteLine("2) Local configuration (client)");
        //Console.WriteLine("3) Remote configuration (server)");
        //Console.WriteLine("4) Remote configuration (client)");
        Console.Write("0) Quit\n\n");

        Console.Write("> ");
        switch (Console.ReadLine())
        {
            case "1":
                Console.WriteLine();
                LocalServerConfiguration();
                break;

            case "2":
                break;

            case "3":
                break;

            case "4":
                break;

            case "0":
                Console.WriteLine();
                Console.WriteLine("Closing...");
                Thread.Sleep(1000);
                Environment.Exit(0);
                break;

            default:
                Console.WriteLine("Invalid op type");
                break;
        }
    }

    static void PrintSTDLine(string cmsg, string msg, bool green = true)
    {
        ConsoleColor cc = ConsoleColor.Green;
        if (!green)
            cc = ConsoleColor.Red;

        Console.ForegroundColor = cc;
        Console.Write("|{0}|", cmsg);
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(" - {0}", msg);
    }

    static void LocalServerConfiguration()
    {
        Console.WriteLine("CONFIGURATION FILES PATH: (file will be created if not found) ");
        string cFilePath = Console.ReadLine();
        if (cFilePath == string.Empty || !cFilePath.EndsWith("pmsconfig.enc"))
            PrintSTDLine("ERR", "Configuration file have to be named \"pmsconfig.enc\"", false);
        else
        {
            if (File.Exists(cFilePath))
            {
                PrintSTDLine("OK", "File Exists");
                
            }
            else
            {
                PrintSTDLine("ERR", "File Not Exists", false);
                File.Create(cFilePath);
                PrintSTDLine("OK", "File Created");
            }
        }
    }
}