using System.Data.Common;
using System.Management.Automation;
using Npgsql;

namespace InvokeQuery
{
    [Cmdlet("Invoke", "PostgreSqlQuery", SupportsTransactions = true, SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
    public class InvokePostgreSqlQuery : InvokeQueryBase
    {
        protected override DbProviderFactory GetProviderFactory()
        {
            return NpgsqlFactory.Instance;
        }
    }
}
