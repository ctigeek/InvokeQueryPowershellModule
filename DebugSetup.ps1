Write-Host "Importing module...."
Import-Module .\InvokeQuery.dll
Write-Host "Hello world"

## CREATE TABLE [dbo].[sometable]([id] [int] NOT NULL,	[somestring] [nvarchar](255) NOT NULL,	[someint] [int] NOT NULL,	[somedate] [datetime2](7) NOT NULL, CONSTRAINT [PK_sometable] PRIMARY KEY CLUSTERED ([id] ASC))
##

$db = "test"
#$sql1 = "update table1 set someint = 4321 where pk = '1A9F96AE-54FA-40F9-9069-749F3ACF0CEF';"
#$sql2 = "UPDATE table1 set someint = 1234 where pk = '89F21B61-891B-42CD-9BED-84BA4B7EB7D0';"

#$count = ($sql1,$sql2) | Invoke-SqlServerQuery -Database $db -Verbose
#Write-Host "Count $count."

$sql = "select * from table1 where somestring = ' update insert delete UPDATE INSERT DELETE ';"
$result = $sql | Invoke-SqlServerQuery -Database $db -Verbose
$result | ft

#$query = "select * from sometable;"
#$query | Invoke-SqlServerQuery -Database "test" -Verbose

##$params = @{"id"=7; "str"="hello"; "inte"=44; "dt"=(Get-Date); "blah"="blah"}

#156..160 | %{
#	[pscustomobject] @{
#		"Sql" = "insert into sometable values (@id, @str, @inte, @dt);";
#		"Parameters" = @{ "id"=$_; "str"="hi"; "inte"=$_ + 99; "dt"=$(Get-Date); }
#	}
#} | Invoke-SqlServerQuery -Database "test" -CUD -Verbose


#171..175 | %{
#	New-SqlQuery -Sql "insert into sometable values (@id, @str, @inte, @dt);" -ExpectedRowCount 0  -Parameters @{ "id"=$_; "str"="hi"; "inte"=$_ + 99; "dt"=$(Get-Date); } -CUD
#	} | Invoke-SqlServerQuery -Database "test" -Verbose

##$query = "insert into sometable values (@id, @str, @inte, @dt);"
##$query | Invoke-SqlServerQuery -Database "test" -Parameters $params -NonQuery -Verbose

