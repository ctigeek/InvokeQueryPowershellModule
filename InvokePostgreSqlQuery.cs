using System;
using System.Data.Common;
using System.Management.Automation;
using Npgsql;

namespace InvokeQuery
{
    [Cmdlet("Invoke", "PostgreSqlQuery")]
    public class InvokePostgreSqlQuery : InvokeQueryBase
    {
        protected override DbConnection CreateConnection(string connectionString)
        {
            return new NpgsqlConnection(connectionString);
        }

        protected override DbCommand CreateCommand(string sql, DbConnection connection)
        {
            return new NpgsqlCommand(sql, (NpgsqlConnection)connection);
        }
    }
}
