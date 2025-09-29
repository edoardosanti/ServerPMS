using ServerPMS.Users;

namespace ServerPMS.Users
{
    public enum UserRole
    {
        Admin,
        System,
        ProductionManager,
        ProductionOperator
    }

    public record class User
    {
        public string RuntimeID { private set; get; }
        public int? DBId { private set; get; }
        public string Username => _username;
        private readonly string _username;

        public string PswHash => _hash.Clone() as string;
        private string _hash;

        public DateTime? LastAccess { set; get; }
        public UserRole Role { private set; get; }


        public User(string username, string pswHash, UserRole role, int? dbID, DateTime? lastAccess)
        {
            DBId = dbID;
            _username = username;
            _hash = pswHash;
            RuntimeID = Guid.NewGuid().ToString();
            Role = role;
            LastAccess = lastAccess;

        }

        public User(string username, string pswHash, int? dbID)
        {
            DBId = dbID;
            _username = username;
            _hash = pswHash;
            RuntimeID = Guid.NewGuid().ToString();
            Role = UserRole.ProductionOperator;

        }
    }

}