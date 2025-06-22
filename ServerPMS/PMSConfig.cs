// PMS Project V1.0
// LSData - all rights reserved
// PMSSettings.cs
//
//
using System;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.Json;
using DocumentFormat.OpenXml.Math;
using System.Data.Common;

namespace ServerPMS
{
    public class SQLiteDatabaseConf
    {
        public string FilePath { get; set; }
        public int Timeout { get; set; }
    }

    public class WALConf
    {
        public string WALFilePath { get; set; }
    }

    public class PEConf
    {
        public ProdUnitConf[] units { get; set; }
    }

    public class ProdUnitConf
    {
        public UnitType type { get; set; }
        public int DBId { get; set; }
    }

    public class UserConf
    {
        public int DBId { get; set; }
    }

    public class Personnel
    {
        public UserConf[] users { get; set; }
    }

    public class PMSConfig
    {
   

        public SDT SoftwareDescTable { get; set; }
        public SQLiteDatabaseConf Database { get; set; }
        public WALConf WAL { set; get; }
        public PEConf ProdEnv {get; set;}
        public Personnel Users { get; set; }

        
    }

}

