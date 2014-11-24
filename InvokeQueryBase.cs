using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data.Common;
using System.Dynamic;
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
            progressRecord = new ProgressRecord(1, "Running Queries...", "");
            progressRecord.RecordType = ProgressRecordType.Processing;
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
        public SwitchParameter UseTransaction { get; set; }

        [Parameter]
        public string Database { get; set; }

        [Parameter]
        public string Server { get; set; }

        protected DbConnection Connection { get; private set; }
        protected DbTransaction Transaction { get; private set; }
        protected int QueryNumber { get; set; }
        private ProgressRecord progressRecord;

        protected abstract DbConnection CreateConnection(string connectionString);
        protected abstract DbCommand CreateCommand(string sql, DbConnection connection);

        protected override void BeginProcessing()
        {
            WriteVerbose("Opening connection...");
            Connection = CreateConnection(CreateConnectionString());
            Connection.Open();
            if (UseTransaction)
            {
                WriteVerbose("Creating Transaction...");
                Transaction = Connection.BeginTransaction();
            }
        }

        protected override void ProcessRecord()
        {
            try
            {
                QueryNumber++;
                progressRecord.CurrentOperation = "Running query number " + QueryNumber.ToString();
                WriteProgress(progressRecord);

                var command = CreateCommand(Query, Connection);
                if (Scalar)
                {
                    WriteVerbose("Running scalar query...");
                    var result = command.ExecuteScalar();
                    WriteVerbose("Query complete. Retrieved scalar result:" + result.ToString());
                    WriteObject(result);
                }
                else if (NonQuery)
                {
                    WriteVerbose("Running NonQuery query...");
                    var result = command.ExecuteNonQuery();
                    WriteVerbose("NonQuery query complete. " + result + " rows affected.");
                    WriteObject(result);
                }
                else
                {
                    WriteDebug("Running query....");
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                dynamic result = new ExpandoObject() as IDictionary<string, Object>;
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    result.Add(reader.GetName(i), reader[i]);
                                }
                                WriteObject(result);
                            }
                        }
                        else
                        {
                            WriteObject(null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StopProcessing();
                throw;
            }
        }

        protected override void EndProcessing()
        {
            progressRecord.RecordType = ProgressRecordType.Completed;
            progressRecord.CurrentOperation = "Committing Transaction...";
            WriteProgress(progressRecord);

            CommitTransaction();
            CloseConnection();
        }

        protected override void StopProcessing()
        {
            progressRecord.RecordType = ProgressRecordType.Completed;
            progressRecord.CurrentOperation = "Stopping...  Rolling back Transaction...";
            WriteProgress(progressRecord);

            RollbackTransaction();
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

        private void RollbackTransaction()
        {
            if (Transaction != null)
            {
                WriteVerbose("Rolling back transaction...");
                Transaction.Rollback();
                Transaction.Dispose();
                Transaction = null;
            }
        }

        private void CommitTransaction()
        {
            if (Transaction != null)
            {
                WriteVerbose("Committing transaction...");
                Transaction.Commit();
                Transaction.Dispose();
                Transaction = null;
            }
        }

        private string CreateConnectionString()
        {
            var formatString = "Data Source={0};Initial Catalog={1};User ID={2};Password={3};Connection Timeout={4}";
            var connectionString = string.Format(formatString, Server, Database, Credential.UserName, Credential.Password, ConnectionTimeout );
            return connectionString;
        }
    }
}
