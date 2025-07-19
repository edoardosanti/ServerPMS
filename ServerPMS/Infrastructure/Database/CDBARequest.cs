using System;
namespace ServerPMS.Infrastructure.Database
{
    //rapresents a request for command execution
    public class CDBARequest
    {
        public string Sql { get; set; } = string.Empty;
        public TaskCompletionSource CompletionSource { get; } = new();
        public Guid? TransactionID { get; set; }
        public CDBARequestType Type { get; set; }

    }
}

