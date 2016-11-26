using System;
using System.Collections;
using System.Management.Automation;

namespace InvokeQuery
{
    public class SqlQuery : PSObject
    {
        public SqlQuery(string sql, int commandTimeout, bool cud, Hashtable parameters, bool scalar, bool storedProcedure, int expectedRowCount, ScriptBlock callback)
        {
            this.Properties.Add(new PSNoteProperty(nameof(Sql), sql));
            this.Properties.Add(new PSNoteProperty(nameof(CommandTimeout), commandTimeout));
            this.Properties.Add(new PSNoteProperty(nameof(CUD), cud));
            this.Properties.Add(new PSNoteProperty(nameof(Parameters), parameters));
            this.Properties.Add(new PSNoteProperty(nameof(Scalar), scalar));
            this.Properties.Add(new PSNoteProperty(nameof(StoredProcedure), storedProcedure));
            this.Properties.Add(new PSNoteProperty(nameof(ExpectedRowCount), expectedRowCount));
            this.Properties.Add(new PSNoteProperty(nameof(Callback), callback));
        }

        public string Sql => (string) this.Properties[nameof(Sql)].Value;

        public int CommandTimeout => (int) this.Properties[nameof(CommandTimeout)].Value;

        public bool CUD => (bool) this.Properties[nameof(CUD)].Value;

        public Hashtable Parameters => (Hashtable) this.Properties[nameof(Parameters)].Value;

        public bool Scalar => (bool)this.Properties[nameof(Scalar)].Value;

        public bool StoredProcedure => (bool) this.Properties[nameof(StoredProcedure)].Value;

        public int ExpectedRowCount => (int) this.Properties[nameof(ExpectedRowCount)].Value;

        public ScriptBlock Callback => (ScriptBlock) this.Properties[nameof(Callback)].Value;
    }
}
