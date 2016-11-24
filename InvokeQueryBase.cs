using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;

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
        
        [Parameter(ParameterSetName = "SqlQuery", Mandatory = true, ValueFromPipeline = true, HelpMessage = "Use New-SqlQuery to create a SqlQuery object.")]
        public SqlQuery SqlQuery { get; set; }

        [Parameter(ParameterSetName = "Default", Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string Sql { get; set; }

        [Parameter(ParameterSetName = "Default")]
        public Hashtable Parameters { get; set; }

        [Parameter]
        [Credential]
        public PSCredential Credential { get; set; }

        [Parameter(ParameterSetName = "Default")]
        public int CommandTimeout { get; set; }

        [Parameter]
        public int ConnectionTimeout { get; set; }

        [Parameter(ParameterSetName = "Default")]
        public SwitchParameter Scalar { get; set; }

        [Parameter(ParameterSetName = "Default")]
        public SwitchParameter CUD { get; set; }

        [Parameter(ParameterSetName = "Default")]
        public SwitchParameter StoredProcedure { get; set; }

        [Parameter]
        public string Database { get; set; }

        [Parameter]
        public string Server { get; set; }

        [Parameter]
        public string ConnectionString { get; set; }

        [Parameter]
        public SwitchParameter NoTrans { get; set; }

        [Parameter(ParameterSetName = "Default")]
        public int ExpectedRowCount { get; set; } = -1;

        protected DbProviderFactory ProviderFactory { get; set; }
        protected DbConnection Connection { get; private set; }
        protected int QueryNumber { get; set; }
        protected ProgressRecord ProgressRecord { get; private set; }
        protected DbTransaction Transaction { get; private set; }
        private Stopwatch stopwatch { get; set; }
        private PSTransactionContext TransactionContext { get; set; }
        private int actualRowCount { get; set; }

        protected override void BeginProcessing()
        {
            if (SqlQuery == null && ExpectedRowCount >= 0 && !CUD)
            {
                throw new PSArgumentException("You can only specify an expected number of rows for a CUD operation. Did you forget the CUD switch?");
            }

            if (SqlQuery == null &&  CUD && Scalar)
            {
                throw new PSArgumentException("You cannot use both the CUD and the Scalar switches at the same time.");
            }

            if (!string.IsNullOrEmpty(ConnectionString) && (!string.IsNullOrEmpty(Server) || !string.IsNullOrEmpty(Database) || ConnectionTimeout > 0))
            {
                throw new PSArgumentException("If you specify a connection string, the Server, Database, and ConnectionTimeout parameter should not be used.");
            }
            stopwatch = Stopwatch.StartNew();
            ConfigureServerProperty();
            ConfigureConnectionString();
            WriteVerbose("Using the following connection string: " + ScrubConnectionString(ConnectionString));
            
            ProviderFactory = GetProviderFactory();

            WriteVerbose("Opening connection.");
            Connection = GetDbConnection();
            EnlistAmbiantTransaction();
            WriteVerbose("Connection to database is open.");
            actualRowCount = 0;
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
                if (SqlQuery == null)
                {
                    SqlQuery = new SqlQuery(Sql, CommandTimeout, CUD, Parameters, StoredProcedure, -1);
                }

                QueryNumber++;
                ProgressRecord.CurrentOperation = "Running query number " + QueryNumber.ToString();
                WriteProgress(ProgressRecord);
                WriteVerbose("Running query number " + QueryNumber.ToString());
                WriteVerbose("Running the following query: " + SqlQuery.Sql);

                if (Scalar)
                {
                    RunScalarQuery();
                }
                else if (SqlQuery.CUD)
                {
                    RunNonQuery();
                }
                else
                {
                    RunQuery();
                }
            }
            catch
            {
                CloseTransaction(false);
                StopProcessing();
                throw;
            }
            SqlQuery = null;
        }

        protected virtual DbCommand GetDbCommand()
        {
            var command = ProviderFactory.CreateCommand();
            command.Connection = Connection;
            command.CommandText = SqlQuery.Sql;
            if (SqlQuery.CommandTimeout > 0)
            {
                command.CommandTimeout = SqlQuery.CommandTimeout;
            }
            if (SqlQuery.StoredProcedure)
            {
                command.CommandType = CommandType.StoredProcedure;
            }
            if (SqlQuery.Parameters != null)
            {
                foreach (var key in SqlQuery.Parameters.Keys)
                {
                    var obj = SqlQuery.Parameters[key];
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
            if (Transaction == null && SqlQuery.CUD && TransactionContext == null && !NoTrans)
            {
                WriteVerbose("Starting transaction.");
                Transaction = Connection.BeginTransaction();
            }
            if (Transaction != null)
            {
                command.Transaction = Transaction;
            }
            return command;
        }

        private void RunQuery()
        {
            var regexPattern = @"(?<=^([^']|'[^']*')*)(update |insert |delete |merge )";
            var uhOh = Regex.Match(SqlQuery.Sql.ToLower(), regexPattern, RegexOptions.Multiline | RegexOptions.IgnoreCase).Success;
            var cudWarning = "WARNING! The following query appears to contain an INSERT/UPDATE/DELETE/MERGE operation, but the CUD switch was not used (which is recommended). Are you sure you want to execute this query?";

            if (uhOh && !ShouldContinue(SqlQuery.Sql, cudWarning))
            {
                WriteWarning("Not running query!");
                return;
            }
            if (!ShouldProcess("Database server", "Run Query:`" + SqlQuery.Sql + "`"))
            {
                WriteWarning("Not running query!");
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
            if (ShouldProcess("Database server", "Run CUD Query:`" + SqlQuery.Sql + "`"))
            {
                var command = GetDbCommand();
                var result = command.ExecuteNonQuery();

                if (SqlQuery.ExpectedRowCount >= 0 && result != SqlQuery.ExpectedRowCount)
                {
                    throw new PSInvalidOperationException("The ExpectedRowCount is " + SqlQuery.ExpectedRowCount + ", but this query had a row count of " + result + " rows. Rolling back the transaction.");
                }
                WriteVerbose("CUD query complete. " + result + " rows affected.");
                actualRowCount += result;
            }
        }

        private void RunScalarQuery()
        {
            if (ShouldProcess("Database server", "Run Scalar Query: `" + SqlQuery.Sql + "`"))
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

        private void EnlistAmbiantTransaction()
        {
            if (TransactionAvailable())
            {
                WriteVerbose("Creating DB connection in established transaction scope.");
                // Important! By simply fetching the CurrentPSTransaction parameter, it moves the powershell ambient transaction into the current transaction. Only then can we enlist the connection.
                //https://blogs.msdn.microsoft.com/powershell/2008/05/08/powershell-transactions-quickstart/
                TransactionContext = CurrentPSTransaction;
                Connection.EnlistTransaction(System.Transactions.Transaction.Current);
            }
        }

        protected override void EndProcessing()
        {
            StopProcessing();
        }

        protected override void StopProcessing()
        {
            var rollback = CUD && ExpectedRowCount >= 0 && actualRowCount != ExpectedRowCount;
            if (ProgressRecord.RecordType != ProgressRecordType.Completed)
            {
                ProgressRecord.RecordType = ProgressRecordType.Completed;
                ProgressRecord.CurrentOperation = "Closing connection...";
                WriteProgress(ProgressRecord);
            }

            CloseTransaction(rollback);
            CloseConnection();
            if (stopwatch != null)
            {
                stopwatch.Stop();
                WriteVerbose("Processed " + QueryNumber + " queries in " + stopwatch.ElapsedMilliseconds + " milliseconds.");
                stopwatch = null;
                WriteVerbose("Complete.");
                if (rollback)
                {
                    throw new PSInvalidOperationException("The ExpectedRowCount is " + ExpectedRowCount + ", but this query had a row count of " + actualRowCount + " rows. Rolling back the transaction.");
                }
                if (CUD)
                {
                    WriteObject(actualRowCount);
                }
            }
        }

        private void CloseTransaction(bool rollback)
        {
            if (TransactionContext != null)
            {
                TransactionContext.Dispose();
                TransactionContext = null;
            }
            if (Transaction != null)
            {
                if (!rollback)
                {
                    WriteVerbose("Committing transaction.");
                    Transaction.Commit();
                }
                else
                {
                    WriteWarning("Rolling back transaction!");
                    Transaction.Rollback();
                }
                Transaction.Dispose();
                Transaction = null;
            }
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
                WriteVerbose("Closing connection.");
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
