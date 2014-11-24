using System;
using System.Data;
using System.Data.Common;
using System.Management.Automation;
using MySql.Data.MySqlClient;

namespace InvokeQuery
{
    [Cmdlet("Invoke","MySqlQuery")]
    public  class InvokeMySqlQuery : InvokeQueryBase
    {
        protected override DbConnection CreateConnection(string connectionString)
        {
            return new MySqlConnection(connectionString);
        }

        protected override DbCommand CreateCommand(string sql, DbConnection connection)
        {
            return new MySqlCommand(sql, (MySqlConnection) connection);
        }
    }
}
