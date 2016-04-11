using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Management.Automation;
using System.Text;

namespace InvokeQuery
{
    public abstract class InvokeQueryBase : PSCmdlet, IDisposable
    {
        protected InvokeQueryBase()
        {
            ConnectionTimeout = -1;
            CommandTimeout = -1;
            Credential = PSCredential.Empty;
            QueryNumber = 0;
            ProgressRecord = new ProgressRecord(1, "Running Queries...", "Running...");
            ProgressRecord.RecordType = ProgressRecordType.Processing;
        }

        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public string Query { get; set; }

        [Parameter]
        [Credential]
        public PSCredential Credential { get; set; }

        [Parameter]
        public int CommandTimeout { get; set; }

        [Parameter]
        public int ConnectionTimeout { get; set; }

        [Parameter]
        public SwitchParameter Scalar { get; set; }

        [Parameter]
        public SwitchParameter NonQuery { get; set; }

        [Parameter]
        public SwitchParameter StoredProcedure { get; set; }

        [Parameter]
        public string Database { get; set; }

        [Parameter]
        public string Server { get; set; }

        [Parameter]
        public string ConnectionString { get; set; }

        [Parameter]
        public Hashtable Parameters { get; set; }

        protected DbProviderFactory ProviderFactory { get; set; }
        protected DbConnection Connection { get; private set; }
        protected int QueryNumber { get; set; }
        protected ProgressRecord ProgressRecord { get; private set; }

        protected override void BeginProcessing()
        {
            if (NonQuery && Scalar)
            {
                throw new ArgumentException("You cannot use both the NonQuery and the Scalar switches at the same time.");
            }

            if (!string.IsNullOrEmpty(ConnectionString) && (!string.IsNullOrEmpty(Server) || !string.IsNullOrEmpty(Database) || ConnectionTimeout > 0))
            {
                throw new ArgumentException("If you specify a connection string, the Server, Database, and ConnectionTimeout parameter should not be used.");
            }

            ConfigureServerProperty();
            ConfigureConnectionString();
            WriteVerbose("Using the following connection string: " + ScrubConnectionString(ConnectionString));
            
            ProviderFactory = GetProviderFactory();

            WriteVerbose("Opening connection...");
            Connection = GetDbConnection();
            WriteVerbose("Connection to database is open.");

            //TODO: do we need to start the transaction here if there are multiple queries?
        }

        protected abstract DbProviderFactory GetProviderFactory();

        protected virtual void ConfigureServerProperty()
        {
            if (string.IsNullOrEmpty(Server))
            {
                Server = "localhost";
            }
        }

        protected virtual DbConnection GetDbConnection()
        {
            var connection = ProviderFactory.CreateConnection();
            if (connection == null)
            {
                throw new ApplicationException("Unable to create conneciton. Please make sure all DLL libraries have been installed.");
            }
            connection.ConnectionString = ConnectionString;
            connection.Open();
            return connection;
        }

        protected override void ProcessRecord()
        {
            try
            {
                QueryNumber++;
                ProgressRecord.CurrentOperation = "Running query number " + QueryNumber.ToString();
                WriteProgress(ProgressRecord);
                WriteVerbose("Running query number " + QueryNumber.ToString());
                WriteVerbose("Running the following query: " + Query);

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

        protected virtual DbCommand GetDbCommand()
        {
            var command = ProviderFactory.CreateCommand();
            command.Connection = Connection;
            command.CommandText = Query;
            if (CommandTimeout > 0)
            {
                command.CommandTimeout = CommandTimeout;
            }
            if (StoredProcedure)
            {
                command.CommandType = CommandType.StoredProcedure;
            }
            if (Parameters != null)
            {
                foreach (var key in Parameters.Keys)
                {
                    var obj = Parameters[key];
                    var pso = obj as PSObject;
                    if (pso != null)
                    {
                        obj = pso.ImmediateBaseObject;
                    }
                    var param = ProviderFactory.CreateParameter();
                    param.ParameterName = key.ToString();
                    param.Value = obj;
                    command.Parameters.Add(param);
                    WriteVerbose("Adding parameter " + param.ParameterName + "=" + param.Value.ToString());
                }
            }
            return command;
        }

        private void RunQuery()
        {
            if (ShouldProcess("Database server", "Run Query:`" + Query + "`"))
            {
                var command = GetDbCommand();
                var dataTable = new DataTable();
                var adapter = ProviderFactory.CreateDataAdapter();
                adapter.SelectCommand = command;
                using (adapter)
                {
                    adapter.Fill(dataTable);
                }
                WriteVerbose("Query returned " + dataTable.Rows.Count + " rows.");
                WriteObject(GetDataRowArrayFromTable(dataTable));
            }
            else
            {
                WriteObject(new DataTable[0]);
            }
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
            if (ShouldProcess("Database server", "Run Non-Query:`" + Query + "`"))
            {
                var command = GetDbCommand();
                var result = command.ExecuteNonQuery();
                WriteVerbose("NonQuery query complete. " + result + " rows affected.");
                WriteObject(result);
            }
            else
            {
                WriteObject(0);
            }
        }

        private void RunScalarQuery()
        {
            if (ShouldProcess("Database server", "Run Scalar Query: `" + Query + "`"))
            {
                var command = GetDbCommand();
                var result = command.ExecuteScalar();
                if (result != null)
                {
                    WriteVerbose("Scalar Query complete. Retrieved result:" + result.ToString());
                }
                else
                {
                    WriteVerbose("Scalar Query complete. Nothing was returned.");
                }
                WriteObject(result);
            }
            else
            {
                WriteObject(0);
            }
        }

        private IDisposable GetTransacitonScope()
        {
            IDisposable transactionScope = NullDisposable.Instance;
            if (TransactionAvailable())
            {
                WriteVerbose("Running query in transaction...");
                transactionScope = CurrentPSTransaction;
            }
            return transactionScope;
        }

        protected override void EndProcessing()
        {
            StopProcessing();
        }

        protected override void StopProcessing()
        {
            if (ProgressRecord.RecordType != ProgressRecordType.Completed)
            {
                ProgressRecord.RecordType = ProgressRecordType.Completed;
                ProgressRecord.CurrentOperation = "Closing connection...";
                WriteProgress(ProgressRecord);
                WriteVerbose("Complete...");
            }
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
                if (Connection.State == ConnectionState.Open)
                {
                    Connection.Close();
                }
                Connection.Dispose();
                Connection = null;
            }
        }

        protected virtual void ConfigureConnectionString()
        {
            if (!string.IsNullOrEmpty(ConnectionString)) return;

            var connString = new StringBuilder();
            connString.AppendFormat("Data Source={0};", Server);
            if (!string.IsNullOrEmpty(Database))
            {
                connString.AppendFormat("Initial Catalog={0};", Database);
            }
            if (Credential != PSCredential.Empty)
            {
                connString.AppendFormat("User ID={0};Password={1};", Credential.UserName, Credential.Password.ConvertToUnsecureString());
            }
            if (ConnectionTimeout > 0)
            {
                connString.AppendFormat("Connection Timeout={0};", ConnectionTimeout);
            }
            ConnectionString = connString.ToString();
        }

        private string ScrubConnectionString(string connectionString)
        {
            var index = connectionString.IndexOf("assword=");
            if (index >= 0)
            {
                index += 8;
                var semiIndex = connectionString.IndexOf(";", index);
                if (index < connectionString.Length)
                {
                    if (semiIndex > 0)
                    {
                        var password = connectionString.Substring(index, semiIndex - index);
                        return connectionString.Replace(password, "xxxxxxxxxx");
                    }
                    else
                    {
                        var password = connectionString.Substring(index);
                        return connectionString.Replace(password, "xxxxxxxxxx");
                    }
                }
                return connectionString;
            }
            else
            {
                return connectionString;
            }
        }
    }
}
