using System.Data.Common;
using System.Data.Odbc;
using System.Management.Automation;

namespace InvokeQuery
{
    [Cmdlet("Invoke", "OdbcQuery", SupportsTransactions = true, SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
    public class InvokeOdbcQuery : InvokeQueryBase
    {
        protected override DbProviderFactory GetProviderFactory()
        {
            return OdbcFactory.Instance;
        }

        [Parameter]
        public string Dsn { get; set; }

        protected override void BeginProcessing()
        {
            if (!string.IsNullOrEmpty(Dsn) && (!string.IsNullOrEmpty(ConnectionString) || !string.IsNullOrEmpty(Server)))
            {
                throw new PSArgumentException("You cannot include a connection string or a server parameter if you also include a DSN parameter.");
            }
            base.BeginProcessing();
        }
        protected override void ConfigureConnectionString()
        {
            base.ConfigureConnectionString();
            if (!string.IsNullOrEmpty(Dsn))
            {
                ConnectionString = ConnectionString + ";DSN=" + Dsn + ";";
            }
        }
    }
}
