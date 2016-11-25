using System;
using System.Collections;
using System.Management.Automation;

namespace InvokeQuery
{
    public class SqlQuery : PSObject
    {
        public SqlQuery(string sql, int commandTimeout, bool cud, Hashtable parameters, bool storedProcedure, int expectedRowCount, ScriptBlock callback)
        {
            this.Properties.Add(new PSNoteProperty("Sql", sql));
            this.Properties.Add(new PSNoteProperty("CommandTimeout", commandTimeout));
            this.Properties.Add(new PSNoteProperty("CUD", cud));
            this.Properties.Add(new PSNoteProperty("Parameters", parameters));
            this.Properties.Add(new PSNoteProperty("StoredProcedure", storedProcedure));
            this.Properties.Add(new PSNoteProperty("ExpectedRowCount", expectedRowCount));
            if (callback != null)
            {
                this.Properties.Add(new PSScriptProperty("Callback", callback));
            }
        }

        public string Sql
        {
            get { return (string) this.Properties["Sql"].Value; }
        }

        public int CommandTimeout
        {
            get { return (int) this.Properties["CommandTimeout"].Value; }
        }

        public bool CUD
        {
            get { return (bool) this.Properties["CUD"].Value; }
        }

        public Hashtable Parameters
        {
            get { return (Hashtable) this.Properties["Parameters"].Value; }
        }

        public bool StoredProcedure
        {
            get { return (bool) this.Properties["StoredProcedure"].Value; }
        }

        public int ExpectedRowCount
        {
            get { return (int) this.Properties["ExpectedRowCount"].Value; }
        }

        public ScriptBlock Callback
        {
            get { return (ScriptBlock) this.Properties["Callback"].Value; }
        }
    }
}
