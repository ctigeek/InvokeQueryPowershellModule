using System;
using System.Data.Common;
using System.Management.Automation;
using Oracle.ManagedDataAccess.Client;

namespace InvokeQuery
{
    [Cmdlet("Invoke", "OracleQuery", SupportsTransactions = true, SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
    public  class InvokeOracleQuery : InvokeQueryBase
    {
        protected override DbProviderFactory GetProviderFactory()
        {
            try
            {
                return OracleClientFactory.Instance;
            }
            catch (Exception ex)
            {
                WriteVerbose("What exception will this throw if the Oracle DLLs aren't loaded? " + ex.ToString());
                throw;
            }
        }
    }
}
