using System.Management.Automation;
using System.Data.SQLite;

namespace InvokeQuery
{
    [Cmdlet("New", "SqliteDatabase", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
    public class NewSqliteDatabase : NewDatabaseBase
    {
        protected override void CreateDatabase()
        {
            SQLiteConnection.CreateFile(FileName);
            using (var connection = new SQLiteConnection("Data Source="+FileName))
            {
                connection.Open();
                var query = "create table test1 (column1 int not null);";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
