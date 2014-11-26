using System.Management.Automation;

namespace InvokeQuery
{
    [Cmdlet("Invoke", "PostgreSqlQuery")]
    public class InvokePostgreSqlQuery : InvokeQueryBase
    {
        private const string NpgsqlDataProvider = "Npgsql.NpgsqlClient";
        public InvokePostgreSqlQuery()
        {
            ProviderInvariantName = NpgsqlDataProvider;
        }
        public sealed override string ProviderInvariantName { get; set; }
    }
}
