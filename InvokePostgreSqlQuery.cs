using System.Management.Automation;

namespace InvokeQuery
{
    [Cmdlet("Invoke", "PostgreSqlQuery", SupportsTransactions = true)]
    public class InvokePostgreSqlQuery : InvokeQueryBase
    {
        private const string NpgsqlDataProvider = "Npgsql.NpgsqlClient";
        public InvokePostgreSqlQuery()
        {
            ProviderInvariantName = NpgsqlDataProvider;
        }
    }
}
