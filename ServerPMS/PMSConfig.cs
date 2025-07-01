// PMS Project V1.0
// LSData - all rights reserved
// PMSSettings.cs
//
//

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
        public long[] UnitsIDs {get; set;}
        public Personnel Users { get; set; }

        public PMSConfig Clone()
        {
            return new PMSConfig
            {
                SoftwareDescTable = this.SoftwareDescTable,
                Database = this.Database.Clone(),
                WAL = this.WAL.Clone(),
                UnitsIDs = this.UnitsIDs?.Clone() as long[],
                Users = this.Users.Clone()
            };
        }
    }
}

