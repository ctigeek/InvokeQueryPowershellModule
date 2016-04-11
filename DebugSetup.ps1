Write-Host "Importing module...."
Import-Module .\InvokeQuery.dll
Write-Host "Hello world"

$query = "select * from sometable;"
$query | Invoke-SqlServerQuery -Database "test" -Verbose


$query = "insert into sometable values (@id, @str, @inte, @dt);"
$params = @{"id"=5; "str"="hello"; "inte"=44; "dt"=(Get-Date)}

$query | Invoke-SqlServerQuery -Database "test" -Parameters $params -NonQuery -Verbose


