using System;
using Microsoft.Extensions.Logging;

namespace ServerPMS.Abstractions.Infrastructure.Database
{
	public interface ICommandDBAccessor
	{
		void Stop();
        Task EnqueueSql(string sql);
        Task EnqueueSql(string sql, Guid CDBATransactionIdentifier);
        Task EnqueueTransactionCommit(Guid CDBATransactionIdentifier);
        Task EnqueueTransactionRollback(Guid CDBATransactionIdentifier);
        Task NewTransactionAndCommit(string[] sqls);
        Guid NewTransaction();

    }
}

