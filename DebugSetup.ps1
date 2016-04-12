Write-Host "Importing module...."
Import-Module .\InvokeQuery.dll
Write-Host "Hello world"

## CREATE TABLE [dbo].[sometable]([id] [int] NOT NULL,	[somestring] [nvarchar](255) NOT NULL,	[someint] [int] NOT NULL,	[somedate] [datetime2](7) NOT NULL, CONSTRAINT [PK_sometable] PRIMARY KEY CLUSTERED ([id] ASC))
##

$query = "select * from sometable;"
$query | Invoke-SqlServerQuery -Database "test" -Verbose

##$params = @{"id"=7; "str"="hello"; "inte"=44; "dt"=(Get-Date); "blah"="blah"}

111..123 | %{
	$querySet = New-Object System.Management.Automation.PSObject
	$querySet | Add-Member -MemberType NoteProperty -Name "Query" -Value "insert into sometable values (@id, @str, @inte, @dt);"
	$querySet | Add-Member -MemberType NoteProperty -Name "Parameters" -Value $(@{ "id"=$_; "str"="hi"; "inte"=$_ + 99; "dt"=(Get-Date); })
	$querySet
} | Invoke-SqlServerQuery -Database "test" -CUD -Verbose

##$query = "insert into sometable values (@id, @str, @inte, @dt);"
##$query | Invoke-SqlServerQuery -Database "test" -Parameters $params -NonQuery -Verbose

