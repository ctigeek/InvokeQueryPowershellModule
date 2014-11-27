using System.Management.Automation;

namespace InvokeQuery
{
    [Cmdlet("Invoke","MySqlQuery", SupportsTransactions = true)]
    public  class InvokeMySqlQuery : InvokeQueryBase
    {
        private const string MySqlProvider = "MySql.Data.MySqlClient";
        public InvokeMySqlQuery()
        {
            ProviderInvariantName = MySqlProvider;
        }
    }
}
