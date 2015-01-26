using System.Management.Automation;

namespace InvokeQuery
{
    public abstract class NewDatabaseBase : PSCmdlet
    {
        [Parameter(Position = 1, ValueFromPipeline = true)]
        public string FileName { get; set; }

        protected override void BeginProcessing()
        {
            CreateDatabase();
        }

        protected abstract void CreateDatabase();
    }
}
