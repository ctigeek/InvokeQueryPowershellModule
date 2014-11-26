using System;
using System.Data;
using System.Data.Common;
using System.Management.Automation;

namespace InvokeQuery
{
    public abstract class InvokeQueryBase : Cmdlet, IDisposable
    {
        protected InvokeQueryBase()
        {
            Server = "localhost";
            ConnectionTimeout = 30;
            CommandTimeout = 60;
            Credential = PSCredential.Empty;
            QueryNumber = 0;
            ProgressRecord = new ProgressRecord(1, "Running Queries...", "");
            ProgressRecord.RecordType = ProgressRecordType.Processing;
        }

        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public virtual string Query { get; set; }

        [Parameter]
        public virtual PSCredential Credential { get; set; }

        [Parameter]
        public virtual int CommandTimeout { get; set; }

        [Parameter]
        public virtual int ConnectionTimeout { get; set; }

        [Parameter]
        public virtual SwitchParameter Scalar { get; set; }

        [Parameter]
        public virtual SwitchParameter NonQuery { get; set; }

        [Parameter]
        public virtual SwitchParameter UseTransaction { get; set; }

        [Parameter]
        public virtual string Database { get; set; }

        [Parameter]
        public virtual string Server { get; set; }

        [Parameter]
        public virtual string ConnectionString { get; set; }

        [Parameter]
        public virtual string ProviderInvariantName { get; set; }

        protected DbProviderFactory ProviderFactory { get; set; }
        protected DbConnection Connection { get; private set; }
        protected int QueryNumber { get; set; }
        protected ProgressRecord ProgressRecord { get; private set; }

        protected override void BeginProcessing()
        {
            ProviderFactory = DbProviderFactories.GetFactory(ProviderInvariantName);
            WriteVerbose("Opening connection...");
            Connection = ProviderFactory.CreateConnection();
            if (Connection == null)
            {
                throw new ArgumentException("Unable to create a Db Provider Factory from provider string `" + ProviderInvariantName + "`.");
            }
            Connection.ConnectionString = CreateConnectionString();
            Connection.Open();
        }

        protected override void ProcessRecord()
        {
            try
            {
                QueryNumber++;
                ProgressRecord.CurrentOperation = "Running query number " + QueryNumber.ToString();
                WriteProgress(ProgressRecord);

                using (GetTransacitonScope())
                {
                    if (Scalar)
                    {
                        RunScalarQuery();
                    }
                    else if (NonQuery)
                    {
                        RunNonQuery();
                    }
                    else
                    {
                        RunQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                StopProcessing();
                throw;
            }
        }

        private void RunQuery()
        {
            WriteDebug("Running query....");
            var command = getDbCommand();
            var dataTable = new DataTable();
            var adapter = ProviderFactory.CreateDataAdapter();
            adapter.InsertCommand = command;
            using (adapter)
            {
                adapter.Fill(dataTable);
            }
            WriteObject(GetDataRowArrayFromTable(dataTable));
        }

        private DataRow[] GetDataRowArrayFromTable(DataTable dataTable)
        {
            var resultSet = new DataRow[dataTable.Rows.Count];
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                resultSet[i] = dataTable.Rows[i];
            }
            return resultSet;
        }

        private void RunNonQuery()
        {
            WriteVerbose("Running NonQuery query...");
            var command = getDbCommand();
            var result = command.ExecuteNonQuery();
            WriteVerbose("NonQuery query complete. " + result + " rows affected.");
            WriteObject(result);
        }

        private void RunScalarQuery()
        {
            WriteVerbose("Running scalar query...");
            var command = getDbCommand();
            var result = command.ExecuteScalar();
            WriteVerbose("Query complete. Retrieved scalar result:" + result.ToString());
            WriteObject(result);
        }

        private DbCommand getDbCommand()
        {
            var command = ProviderFactory.CreateCommand();
            command.Connection = Connection;
            command.CommandText = Query;
            return command;
        }

        private IDisposable GetTransacitonScope()
        {
            IDisposable transactionScope = NullDisposable.Instance;
            if (TransactionAvailable())
            {
                transactionScope = CurrentPSTransaction;
            }
            return transactionScope;
        }

        protected override void EndProcessing()
        {
            ProgressRecord.RecordType = ProgressRecordType.Completed;
            ProgressRecord.CurrentOperation = "Completing Transaction...";
            WriteProgress(ProgressRecord);
            CloseConnection();
        }

        protected override void StopProcessing()
        {
            ProgressRecord.RecordType = ProgressRecordType.Completed;
            ProgressRecord.CurrentOperation = "Stopping...";
            WriteProgress(ProgressRecord);
            CloseConnection();
        }

        public void Dispose()
        {
            StopProcessing();
        }

        private void CloseConnection()
        {
            if (Connection != null)
            {
                Connection.Close();
                Connection.Dispose();
                Connection = null;
            }
        }

        private string CreateConnectionString()
        {
            if (!string.IsNullOrEmpty(ConnectionString)) return ConnectionString;

            var formatString = "Data Source={0};Initial Catalog={1};User ID={2};Password={3};Connection Timeout={4}";
            var connectionString = string.Format(formatString, Server, Database, Credential.UserName, Credential.Password, ConnectionTimeout );
            return connectionString;
        }
    }
}
