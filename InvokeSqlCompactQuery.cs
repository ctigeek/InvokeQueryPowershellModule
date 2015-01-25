using System.Data.Common;
using System.Data.SqlServerCe;
using System.Management.Automation;

namespace InvokeQuery
{
    [Cmdlet("Invoke", "SqlCompactQuery", SupportsTransactions = true, SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
    public  class InvokeSqlCompactQuery : InvokeQueryBase
    {
        protected override DbProviderFactory GetProviderFactory()
        {
            return SqlCeProviderFactory.Instance;
        }
    }
}
