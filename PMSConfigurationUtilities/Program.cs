using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using DocumentFormat.OpenXml.ExtendedProperties;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Office.Word;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Data.Sqlite;
using ServerPMS;
using System.Data.Common;
using System.Data;

namespace PMSConfigurationUtilities;
class Program
{
    static PMSConfig config;
    static string path;
    static bool exit = false;
    static string[] files = { "_cc.env", "pmsconf.enc", "wal.walfile", "test2.db" };

    static void Main(string[] args)
    {
        //debug
        Console.Title = "PMS Configuration";
        Console.WriteLine("PMS Configuration Utility\n\n");


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


    /// <summary>
    /// Print a line with a colored message followed by a white message
    /// </summary>
    static void PrintSTDLine(string cmsg, string msg, ConsoleColor color =  ConsoleColor.Green)
    {
        Console.ForegroundColor = color;
        Console.Write("|{0}|", cmsg);
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(" - {0}", msg);
    }

    /// <summary>
    /// Procedure to configure server on local machine
    /// </summary>
    static void LocalServerConfiguration()
    {

        

        //TODO: ADD AUTO CHECK FOR FILES IN WORKING PATH, IF NOT FOUND ->

        Console.WriteLine("CONFIGURATION FILES DIRECTORY PATH:");
        Console.Write("> ");
        path = Console.ReadLine();
        path = path.EndsWith('/') ? path : path + '/';

        Console.WriteLine("\nSearching files...");
        foreach(string filename in files)
        {
            if (File.Exists(path + filename))
            {
                string msg = string.Format("File Found. ({0})", filename);
                PrintSTDLine("PSD", msg);

            }
            else
            {
                string msg = string.Format("File Not Found. ({0})", filename);
                PrintSTDLine("ERR", msg,ConsoleColor.Red);
                Thread.Sleep(1000);
                Environment.Exit(0);
            }
        }


        Console.WriteLine("\nParsing actual configuration...");
        ConfigCrypto.EnvFilePath = path + files[0];
        
           
        try
        {
            PrintSTDLine("WRN", "Decrypting configuration file.", ConsoleColor.DarkYellow);
            string json = ConfigCrypto.DecryptFromFile(path + files[1]);
            PrintSTDLine("PSD", "Configuration decrypted.");
            PrintSTDLine("WRN", "Parsing configuration.", ConsoleColor.DarkYellow);
            config = JsonSerializer.Deserialize<PMSConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            PrintSTDLine("PSD", "Configuration parsed.");
        }
        catch(JsonException jex)
        {
            PrintSTDLine("ERR", "Configuration parsing failed.", ConsoleColor.Red);
            Thread.Sleep(1000);
            Environment.Exit(0);
        }
        catch(Exception ex)
        {
            PrintSTDLine("ERR", "Decrypting failed.", ConsoleColor.Red);
            Thread.Sleep(1000);
            Environment.Exit(0);
        }


        while (!exit)
        {
            Console.WriteLine("\nSelect operation: \n");
            Console.WriteLine("1) Production Units Configuration");
            Console.WriteLine("2) Network Configuration");
            Console.WriteLine("3) Users Configuration");
            Console.WriteLine("4) General Configuration");
            Console.Write("0) Quit\n\n");

            Console.Write("> ");
            switch (Console.ReadLine())
            {
                case "1":
                    ConfigureProductionUnits();
                    break;

                case "2":
                    break;

                case "3":
                    break;

                case "4":
                    GeneralConfiguration();
                    break;

                case "0":
                    Console.WriteLine();
                    Console.WriteLine("Writing and closing...");
                    string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    Console.WriteLine(json);
                    ConfigCrypto.EncryptToFile(json, path + files[1]);
                    Thread.Sleep(1000);
                    Environment.Exit(0);
                    break;

                default:
                    Console.WriteLine("Invalid op type");
                    break;
            }

        }
    }

    static void ConfigureProductionUnits()
    {

        
        List<long> unitsList = new List<long>();
        if (config.UnitsIDs != null)
        {
            foreach (long ids in config.UnitsIDs)
            {
                unitsList.Add(ids);
            }
        }

        //add unit in units table and get ID

        Console.WriteLine("\nPRODUCTION UNITS: \n");

        if (unitsList.Count >= 1)
        {
            using (SqliteConnection connection = new SqliteConnection(string.Format("Data Source={0};Mode=ReadWrite;", path+files[3])))
            {
                connection.Open();
                foreach (int id in unitsList)
                {
                    using (SqliteCommand cmd = new SqliteCommand(string.Format("SELECT * FROM prod_units WHERE ID= {0}", id),connection))
                    {

                        DbDataReader dbdr = cmd.ExecuteReader();
                        if (dbdr.Read()) {
                            for (int i = 0; i < dbdr.FieldCount; i++)
                            {
                                Console.Write("{0}: {1} | ", dbdr.GetName(i), dbdr.GetValue(i)?.ToString());
                            }
                            Console.WriteLine();
                        }
                        else
                            Console.WriteLine("NULL");
                    }
                }
            }
        }
        else
        {
            Console.WriteLine("!!No production unit found!!");
        }

        Console.WriteLine("\nSelect operation: \n");
        Console.WriteLine("1) Add Unit");
        Console.WriteLine("2) Remove Unit");
        Console.WriteLine("3) Edit Unit");
        Console.Write("0) Quit\n\n");

        Console.Write("> ");
        switch (Console.ReadLine())
        {
            case "1":
                NewUnit(ref unitsList);
                break;

            case "2":
                break;

            case "3":
                break;

            case "4":
                break;

            case "0":
                exit = true;
                break;

            default:
                Console.WriteLine("Invalid op type");
                break;
        }

        config.UnitsIDs = unitsList.ToArray();




    }

    /// <summary>
    /// Add unit to unit table and return its id
    /// </summary>
    /// <returns>int DBId of unit</returns>
    static void NewUnit(ref List<long> units)
    {
        Console.WriteLine("Add Unit\n");
        Console.Write("Name/Identifier: ");
        string name = Console.ReadLine();
        int type;
        try
        {
            Console.Write("Type: ");
            Enum.TryParse<UnitType>(Console.ReadLine(), out var type1);
            type = (int)type1;
        }catch(Exception ex)
        {
            Console.WriteLine("Invalid type. Unit set to default type. (Molding Machine)");
            type = 0;
        }
        Console.Write("Notes: ");
        string notes = Console.ReadLine();

        long DBId;

        //add to table
        using (SqliteConnection connection = new SqliteConnection(string.Format("Data Source={0};Mode=ReadWrite;", path+files[3])))
        {
            connection.Open();
            using (SqliteCommand cmd = new SqliteCommand("INSERT INTO prod_units (name, type, status, notes, current_production_order, current_queue) VALUES (@name, @type,0,@notes,-1,-1) RETURNING ID;",connection))
            {
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@type", type);
                cmd.Parameters.AddWithValue("@notes", notes);
                DBId = (long)cmd.ExecuteScalar();
            }
        }


        units.Add(DBId);
    }

    static void GeneralConfiguration()
    {

    }
}