using System;
using System.Collections;
using System.Management.Automation;

namespace InvokeQuery
{
    [Cmdlet("New","SqlQuery", ConfirmImpact = ConfirmImpact.None)]
    public class NewSqlQuery : PSCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
        public string Sql { get; set; }

        [Parameter]
        public int CommandTimeout { get; set; }

        [Parameter]
        public SwitchParameter CUD { get; set; }

        [Parameter]
        public Hashtable Parameters { get; set; }

        [Parameter]
        public SwitchParameter StoredProcedure { get; set; }

        [Parameter]
        public SwitchParameter Scalar { get; set; }

        [Parameter]
        public int ExpectedRowCount { get; set; } = -1;

        [Parameter]
        public ScriptBlock Callback { get; set; }

        protected override void BeginProcessing()
        {
            if (ExpectedRowCount >= 0 && !CUD)
            {
                throw new PSArgumentException("ExpectedRowCount can only be specified with the CUD switch.");
            }
            base.BeginProcessing();
        }

        protected override void ProcessRecord()
        {
            var query = new SqlQuery(Sql, CommandTimeout, CUD, Parameters, Scalar, StoredProcedure, ExpectedRowCount, Callback);
            WriteObject(query);
        }
    }
}
