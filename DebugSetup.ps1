Write-Host "Importing module...."
Import-Module .\InvokeQuery.dll
Write-Host "Hello world"

## CREATE TABLE [dbo].[sometable]([id] [int] NOT NULL,	[somestring] [nvarchar](255) NOT NULL,	[someint] [int] NOT NULL,	[somedate] [datetime2](7) NOT NULL, CONSTRAINT [PK_sometable] PRIMARY KEY CLUSTERED ([id] ASC))
##

$query = "select * from sometable;"
$query | Invoke-SqlServerQuery -Database "test" -Verbose

$params = @{"id"=7; "str"="hello"; "inte"=44; "dt"=(Get-Date); "blah"="blah"}
#40..35 | %{ "insert into sometable values ($($_), @str, @inte, @dt);" } | Invoke-SqlServerQuery -Database "test" -Parameters $params -CUD -Verbose

##$query = "insert into sometable values (@id, @str, @inte, @dt);"
##$query | Invoke-SqlServerQuery -Database "test" -Parameters $params -NonQuery -Verbose

