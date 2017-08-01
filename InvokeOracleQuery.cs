using System;
using System.Data.Common;
using System.Management.Automation;
using System.Security;
using System.Security.Permissions;
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
                return new OracleDbProviderFactoryWrapper(OracleClientFactory.Instance);
            }
            catch (Exception ex)
            {
                WriteVerbose("What exception will this throw if the Oracle DLLs aren't loaded? " + ex.ToString());
                throw;
            }
        }

        private class OracleDbProviderFactoryWrapper : DbProviderFactory
        {
            public OracleDbProviderFactoryWrapper(OracleClientFactory factory)
            {
                this.factory = factory;
            }


            private OracleClientFactory factory { get; set; }


            public override bool CanCreateDataSourceEnumerator
            {
                get
                {
                    return this.factory.CanCreateDataSourceEnumerator;
                }
            }


            public override DbCommand CreateCommand()
            {
                var command = (OracleCommand)this.factory.CreateCommand();
                command.BindByName = true;
                return command;
            }

            public override DbCommandBuilder CreateCommandBuilder()
            {
                return this.factory.CreateCommandBuilder();
            }

            public override DbConnection CreateConnection()
            {
                return this.factory.CreateConnection();
            }

            public override DbConnectionStringBuilder CreateConnectionStringBuilder()
            {
                return this.factory.CreateConnectionStringBuilder();
            }

            public override DbDataAdapter CreateDataAdapter()
            {
                return this.factory.CreateDataAdapter();
            }

            public override DbDataSourceEnumerator CreateDataSourceEnumerator()
            {
                return this.factory.CreateDataSourceEnumerator();
            }

            public override DbParameter CreateParameter()
            {
                return this.factory.CreateParameter();
            }

            public override CodeAccessPermission CreatePermission(PermissionState state)
            {
                return this.factory.CreatePermission(state);
            }
        }
    }
}
