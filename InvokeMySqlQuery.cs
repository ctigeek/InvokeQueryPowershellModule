using System.Data.Common;
using System.Management.Automation;
using MySql.Data.MySqlClient;

namespace InvokeQuery
{
    [Cmdlet("Invoke", "MySqlQuery", SupportsTransactions = true, SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
    public  class InvokeMySqlQuery : InvokeQueryBase
    {
        protected override DbProviderFactory GetProviderFactory()
        {
            return MySqlClientFactory.Instance;
        }
    }
}
