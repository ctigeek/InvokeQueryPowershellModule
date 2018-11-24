using Npgsql;
using System.Data.Common;
using System.Management.Automation;

namespace InvokeQuery
{
    [Cmdlet("Invoke", "PostgreSqlQuery", SupportsTransactions = true, SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
    public class InvokePostgreSqlQuery : InvokeQueryBase
    {
        protected override DbProviderFactory GetProviderFactory()
        {
            return NpgsqlFactory.Instance;
        }

        protected override void ConfigureConnectionString()
        {
            if (string.IsNullOrEmpty(ConnectionString))
            {
                var connString = new NpgsqlConnectionStringBuilder();
                connString.Host = Server;

                if (!string.IsNullOrEmpty(Database))
                {
                    connString.Database = Database;
                }

                if (Credential != PSCredential.Empty)
                {
                    connString.Username = Credential.UserName;
                    connString.Password = Credential.Password.ConvertToUnsecureString();
                }

                if (ConnectionTimeout > 0)
                {
                    connString.Timeout = ConnectionTimeout;
                }

                ConnectionString = connString.ToString();
            }
        }
    }
}
