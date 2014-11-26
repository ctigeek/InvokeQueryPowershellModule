using System.Management.Automation;

namespace InvokeQuery
{
    [Cmdlet("Invoke", "Query")]
    public class InvokeQuery : InvokeQueryBase
    {
        public InvokeQuery()
        {
            Server = "localhost";
        }
    }
}
