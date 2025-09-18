using System;
using Microsoft.Extensions.Logging;

namespace ServerPMS.Abstractions.Infrastructure.Database
{
	public interface ICommandDBAccessor
	{
        Task StopAsync(CancellationToken cancellationToken);
        Task EnqueueSql(string sql);
        Task EnqueueSql(string sql, Guid CDBATransactionIdentifier);
        Task EnqueueTransactionCommit(Guid CDBATransactionIdentifier);
        Task EnqueueTransactionRollback(Guid CDBATransactionIdentifier);
        Task NewTransactionAndCommit(string[] sqls);
        Guid NewTransaction();
        bool IsRunning { get; }
    }
}

