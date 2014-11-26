using System.Management.Automation;

namespace InvokeQuery
{
    [Cmdlet("Invoke","SqlServerQuery")]
    public class InvokeSqlServerQuery : InvokeQueryBase
    {
        private const string SqlServerProvider = "System.Data.SqlClient";
        public InvokeSqlServerQuery()
        {
            ProviderInvariantName = SqlServerProvider;
        }

        public sealed override string ProviderInvariantName { get; set; }
    }
}
