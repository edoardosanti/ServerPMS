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
using DocumentFormat.OpenXml.Spreadsheet;

namespace ServerPMS
{
    public class SQLiteDatabaseConf:ICloneable<SQLiteDatabaseConf>
    {
        public string FilePath { get; set; }
        public int Timeout { get; set; }

        public SQLiteDatabaseConf Clone()
        {
            return new SQLiteDatabaseConf { FilePath = this.FilePath, Timeout = this.Timeout };
        }
    }

    public class WALConf:ICloneable<WALConf>
    {
        public string WALFilePath { get; set; }

        public WALConf Clone()
        {
            return new WALConf { WALFilePath = this.WALFilePath};
        }
    }

    public class PEConf:ICloneable<PEConf>
    {
        public ProdUnitConf[] units { get; set; }

        public PEConf Clone()
        {
            return new PEConf { units = this.units?.Clone() as ProdUnitConf[]?? null };
        }
    }

    public class ProdUnitConf:ICloneable<ProdUnitConf>
    {
        public UnitType type { get; set; }
        public int DBId { get; set; }

        public ProdUnitConf Clone()
        {
            return new ProdUnitConf { type = this.type, DBId = this.DBId };
        }
    }

    public class UserConf:ICloneable<UserConf>
    {
        public int DBId { get; set; }

        public UserConf Clone()
        {
            return new UserConf { DBId = this.DBId };
        }
    }

    public class Personnel:ICloneable<Personnel>
    {
        public UserConf[] users { get; set; }

        public Personnel Clone()
        {
            return new Personnel { users = this.users?.Clone() as UserConf[] ?? null};
        }
    }

    public class PMSConfig:ICloneable<PMSConfig>
    {
   

        public SDT SoftwareDescTable { get; set; }
        public SQLiteDatabaseConf Database { get; set; }
        public WALConf WAL { set; get; }
        public PEConf ProdEnv {get; set;}
        public Personnel Users { get; set; }

        public PMSConfig Clone()
        {
            return new PMSConfig
            {
                SoftwareDescTable = this.SoftwareDescTable,
                Database = this.Database.Clone(),
                WAL = this.WAL.Clone(),
                ProdEnv = this.ProdEnv.Clone(),
                Users = this.Users.Clone()
            };
        }
    }

}

