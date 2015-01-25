using System.Data.Common;
using System.Data.SQLite;
using System.Management.Automation;

namespace InvokeQuery
{
    [Cmdlet("Invoke", "SqliteQuery", SupportsTransactions = true, SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
    public  class InvokeSqliteQuery : InvokeQueryBase
    {
        protected override DbProviderFactory GetProviderFactory()
        {
            return SQLiteFactory.Instance;
        }
    }
}
