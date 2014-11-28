using System.Management.Automation;

namespace InvokeQuery
{
    [Cmdlet("Invoke", "Query", SupportsTransactions = true, SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
    public class InvokeQuery : InvokeQueryBase
    {
        public InvokeQuery()
        {
        }
    }
}
