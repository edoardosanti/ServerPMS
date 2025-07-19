using System.Data.Common;

namespace ServerPMS.Infrastructure.Database
{
    // Represents a query task with a typed result
    public class QueryRequest<T>
    {
        public string Sql { get; set; } = string.Empty;
        public Func<DbDataReader, T> Parser { get; set; } = default!;
        public TaskCompletionSource<T> CompletionSource { get; } = new();
    }
}

