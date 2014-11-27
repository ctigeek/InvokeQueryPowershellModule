using System.Management.Automation;

namespace InvokeQuery
{
    [Cmdlet("Invoke", "Query", SupportsTransactions = true)]
    public class InvokeQuery : InvokeQueryBase
    {
        public InvokeQuery()
        {
        }
    }
}
