using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace InvokeQuery
{
    [Cmdlet("Invoke","Test", SupportsTransactions = true)]
    public class InvokeTest : Cmdlet
    {
        private ProgressRecord progressRecord;
        private int recordNumber;
        
        public InvokeTest()
        {
            progressRecord = new ProgressRecord(1, "stuff", "not started");
            progressRecord.RecordType = ProgressRecordType.Processing;
            recordNumber = 0;
        }

        [Parameter]
        public SwitchParameter WhatIf { get; set; }

        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public string ProcessThis { get; set; }

        protected override void BeginProcessing()
        {
            progressRecord.PercentComplete = 20;
            progressRecord.StatusDescription = "Beginning.";
            progressRecord.CurrentOperation = "Beginning";
            
            WriteProgress(progressRecord);
            WriteVerbose("blah blah blah");
            WriteDebug("This is debug stuff...");
        }

        protected override void EndProcessing()
        {
            progressRecord.PercentComplete = 100;
            progressRecord.StatusDescription = "Ended.";
            progressRecord.CurrentOperation = "Ended";
            progressRecord.RecordType = ProgressRecordType.Completed;
            WriteProgress(progressRecord);
            WriteVerbose("Done processing...");
        }

        protected override void StopProcessing()
        {
            WriteWarning("I stopped processing!");
        }

        protected override void ProcessRecord()
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    WriteVerbose("in a transaction!");
                }
            }
            recordNumber++;
            progressRecord.PercentComplete = recordNumber + 20;
            progressRecord.StatusDescription = "Processing number " + recordNumber.ToString();
            progressRecord.CurrentOperation = "Processing record number " + recordNumber.ToString();
            WriteProgress(progressRecord);

            if (WhatIf)
            {
                WriteVerbose("Not really processing....");
            }
            else
            {
                WriteVerbose("Processing " + ProcessThis);
                dynamic outputObject = new ExpandoObject();
                outputObject.ThisWasProcessed = ProcessThis;
                outputObject.ThisIsThree = 3;

                WriteObject(outputObject);
            }
            System.Threading.Thread.Sleep(1000);
        }
    }
}
