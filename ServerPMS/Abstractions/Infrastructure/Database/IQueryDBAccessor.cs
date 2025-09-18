using System;
using System.Data.Common;

namespace ServerPMS.Abstractions.Infrastructure.Database
{
	public interface IQueryDBAccessor
	{
        public Task<T> QueryAsync<T>(string sql, Func<DbDataReader, T> parser);
        bool IsRunning { get; }
    }
}

