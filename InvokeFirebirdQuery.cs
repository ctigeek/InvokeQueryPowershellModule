using System.Data.Common;
using System.Management.Automation;
using FirebirdSql.Data.FirebirdClient;

namespace InvokeQuery
{
    [Cmdlet("Invoke", "FirebirdQuery", SupportsTransactions = true, SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
    public  class InvokeFirebirdQuery : InvokeQueryBase
    {
        protected override DbProviderFactory GetProviderFactory()
        {
            return FirebirdClientFactory.Instance;
        }
    }
}
