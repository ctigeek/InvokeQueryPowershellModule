using System.Management.Automation;

namespace InvokeQuery
{
    [Cmdlet("New", "SqlCompactDatabase", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
    public class NewSqlCompactDatabase : NewDatabaseBase
    {
        protected override void CreateDatabase()
        {
            var connstring = "Data Source=" + FileName;
            using (var engine = new System.Data.SqlServerCe.SqlCeEngine(connstring))
            {
                engine.CreateDatabase();
            }
        }
    }
}
