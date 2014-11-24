using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Management.Automation;

namespace InvokeQuery
{
    [Cmdlet("Invoke","SqlServerQuery")]
    public class InvokeSqlServerQuery : InvokeQueryBase
    {
        protected override DbConnection CreateConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }

        protected override DbCommand CreateCommand(string sql, DbConnection connection)
        {
            return new SqlCommand(sql, (SqlConnection) connection);
        }
    }
}
