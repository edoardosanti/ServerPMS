using System;
namespace ServerPMS.Infrastructure.Database
{
    public enum CDBARequestType
    {
        SQLCommand,
        TransactionCommit,
        TransactionRollback
    }
}

