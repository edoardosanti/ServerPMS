using System;
namespace ServerPMS.Users
{
    public enum UserOperationType
    {
        StartProduction,
        StopProduction,
        Changeover,
        EditOrderDetails
    }

    public record class UserOperationDescriptor
    {
        public string RuntimeID { private set; get; }
        public int? DBId { private set; get; }
        public UserOperationType Type { private set; get; }
        public string? UnitID { private set; get; }
        public string? OrderID { private set; get; }
        public string? QueueID { private set; get; }
        public string UserID { private set; get; }

        public readonly DateTime Timestamp;

        public UserOperationDescriptor(string requestingUserID, UserOperationType type, string? unitID = null, string? orderID = null, string? queueID = null, int? dbId = null)
        {
            UserID = requestingUserID;
            RuntimeID = Guid.NewGuid().ToString();
            DBId = dbId;
            Type = type;
            UnitID = unitID;
            OrderID = orderID;
            QueueID = queueID;
        }
    }
}

