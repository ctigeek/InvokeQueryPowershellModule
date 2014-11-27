using System;
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
            CommandTimeout = 60;
            Credential = PSCredential.Empty;
            QueryNumber = 0;
            ProgressRecord = new ProgressRecord(1, "Running Queries...", "Running...");
            ProgressRecord.RecordType = ProgressRecordType.Processing;
        }

        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public string Query { get; set; }

        [Parameter]
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
        public SwitchParameter WhatIf { get; set; }

        [Parameter]
        public string Database { get; set; }

        [Parameter]
        public string Server { get; set; }

        [Parameter]
        public string Port { get; set; }

        [Parameter]
        public string ConnectionString { get; set; }

        [Parameter]
        public string ProviderInvariantName { get; set; }

        protected DbProviderFactory ProviderFactory { get; set; }
        protected DbConnection Connection { get; private set; }
        protected int QueryNumber { get; set; }
        protected ProgressRecord ProgressRecord { get; private set; }

        protected override void BeginProcessing()
        {
            if (WhatIf)
            {
                WriteVerbose("WhatIf is on.  No connection to the db will be made and no queries will actually be run.");
            }
            
            ConfigureServerProperty();
            ConfigureConnectionString();
            WriteVerbose("Using the following connection string: " + ScrubConnectionString(ConnectionString));

            WriteVerbose("Creating DbProvider Factory using the following Invarian Name: " + ProviderInvariantName);
            ProviderFactory = DbProviderFactories.GetFactory(ProviderInvariantName);

            WriteVerbose("Opening connection...");
            Connection = GetDbConnection();
            WriteVerbose("Connection to database is open.");
        }

        protected virtual void ConfigureServerProperty()
        {
            if (string.IsNullOrEmpty(Server))
            {
                Server = "localhost";
            }
        }

        protected virtual DbConnection GetDbConnection()
        {
            if (WhatIf)
            {
                return null;
            }
            var connection = ProviderFactory.CreateConnection();
            if (connection == null)
            {
                throw new ArgumentException("Unable to create a Db Provider Factory from provider string `" + ProviderInvariantName + "`.");
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
            if (WhatIf)
            {
                return null;
            }
            var command = ProviderFactory.CreateCommand();
            command.Connection = Connection;
            command.CommandText = Query;
            if (StoredProcedure)
            {
                command.CommandType = CommandType.StoredProcedure;
            }
            return command;
        }

        private void RunQuery()
        {
            if (WhatIf)
            {
                WriteVerbose("Query was not actually run, so returning 0 rows.");
                WriteObject(new DataTable[0]);
                return;
            }

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
            if (WhatIf)
            {
                WriteVerbose("Query was not actually run, so returning 0 for rows affected.");
                WriteObject(0);
                return;
            }
            var command = GetDbCommand();
            var result = command.ExecuteNonQuery();
            WriteVerbose("NonQuery query complete. " + result + " rows affected.");
            WriteObject(result);
        }

        private void RunScalarQuery()
        {
            if (WhatIf)
            {
                WriteVerbose("Query was not actually run, so returning 0 for value.");
                WriteObject(0);
                return;
            }
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
            if (!string.IsNullOrEmpty(Port))
            {
                connString.AppendFormat("Port={0};", Port);
            }
            if (Credential != PSCredential.Empty)
            {
                connString.AppendFormat("User ID={0};Password={1};", Credential.UserName, Credential.Password.ConvertToUnsecureString());
            }
            else
            {
                connString.Append("Integrated Security=SSPI;");
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
