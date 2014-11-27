function Assert($testnum, $expected, $actual) {
   if ($expected -ne $actual) {
      Write-Error "Error in test # $testnum. Expected $expected but was $actual."
      throw "test error"
   }
}

$secpasswd = ConvertTo-SecureString "92847rivopwkxur" -AsPlainText -Force
$mycreds = New-Object System.Management.Automation.PSCredential ("InvokeSqlLogin", $secpasswd)
$db = "invokequerytest"
$server = "localhost"
$table = "``TestTable1``"

###########################################################################################
## Make sure table is set up correctly....
$query = "delete from $table where id > 3;";
$result = (Invoke-MySqlQuery -Query $query -Database $db -Credential $mycreds -NonQuery -Verbose -Server $server )

######################################################################################################
## test 1, explicitly set the server, database, and credentials
$query = "select 5 as TestRow1;"
$result = (Invoke-MySqlQuery -Query $query  -Database $db -Server $server -Credential $mycreds) 
Assert 1 $result.GetType().Name  "DataRow[]"
Assert 1 $result.Count  1
Assert 1 $result.TestRow1 5

## test 2, use windows creds, don't specify server.

$result = (Invoke-MySqlQuery -Query $query -Database $db -Credential $mycreds ) 
Assert 2 $result.GetType().Name  "DataRow[]"
Assert 2 $result.Count  1
Assert 2 $result.TestRow1 5

## test 3, use scalar

$result = (Invoke-MySqlQuery -Query $query -Database $db -Scalar -Credential $mycreds) 
Assert 3 $result.GetType().Name  "Int64"
Assert 3 $result 5

## test4  select from table....
$query = "select * from $table order by id;" ##the test db set up should have inserted 3 rows.
$result = (Invoke-MySqlQuery -Query $query -Database $db -Credential $mycreds) 
Assert 4 $result.GetType().Name  "DataRow[]"
Assert 4 $result.Count  3
Assert 4 $result[0].id 1
Assert 4 $result[0].somestring "blahblah"
Assert 4 $result[0].someDateTime "2014-11-20" (Get-Date -Year 2014 -Month 11 -Day 20)

## test 5,  insert into table....
$query = "insert into $table (somestring,someInt,someDateTime) values ('newRow',99,'2022-11-20'); "
## when you pass the -NonQuery switch, it returns the number or rows affected.
$result = (Invoke-MySqlQuery -Query $query -Database $db -NonQuery -Credential $mycreds) 
Assert 5 $result.GetType().Name  "Int32"
Assert 5 $result 1

## test 6,  update table....
$query = "update $table set somestring = 'updatedRow' where someInt = 99;"
## when you pass the -NonQuery switch, it returns the number or rows affected.
$result = (Invoke-MySqlQuery -Query $query -Database $db -NonQuery -Credential $mycreds) 
Assert 6 $result.GetType().Name  "Int32"
Assert 6 $result 1

## test 7,  delete table....
$query = "delete from $table where someInt = 99;"
## when you pass the -NonQuery switch, it returns the number or rows affected.
$result = (Invoke-MySqlQuery -Query $query -Database $db -NonQuery -Credential $mycreds) 
Assert 7 $result.GetType().Name  "Int32"
Assert 7 $result 1

# test 8, stored proc
#$query = "TestProc1"
#$result = (Invoke-MySqlQuery -Query $query -Database $db -StoredProcedure ) 
#Assert 8 $result.GetType().Name  "DataRow[]"
#Assert 8 $result.Count  3
#Assert 8 $result[0].id 1
#Assert 8 $result[0].somestring "blahblah"
#Assert 8 $result[0].someDateTime "2014-11-20" (Get-Date -Year 2014 -Month 11 -Day 20)


#test 9, multiple queries...
$query1 = "insert into $table (somestring,someInt,someDateTime) values ('row1',111,'2022-11-21'); "
$query2 = "insert into $table (somestring,someInt,someDateTime) values ('row2',112,'2022-11-22'); "
$query3 = "insert into $table (somestring,someInt,someDateTime) values ('row3',113,'2022-11-23'); "

$result = (($query1, $query2, $query3) | Invoke-MySqlQuery -Database $db -NonQuery -Credential $mycreds)
Assert 9 $result.Count  3
Assert 9 $result[0] 1
Assert 9 $result[1] 1
Assert 9 $result[2] 1

############################################################################################
#test 10, transaction
$query1 = "insert into $table (somestring,someInt,someDateTime) values ('row4',114,'2022-11-24'); "
$query2 = "insert into $table (somestring,someInt,someDateTime) values ('row5',115,'2022-11-25'); "
$query3 = "insert into $table (somestring,someInt,someDateTime) values ('row6',116,'2022-11-26'); "
Start-Transaction
$result = (($query1, $query2, $query3) | Invoke-MySqlQuery -Database $db -NonQuery -UseTransaction -Credential $mycreds)
Assert 10 $result.Count  3
Complete-Transaction

$query = "select count(*) from TestTable1 where someInt in (114,115,116);"
$result = Invoke-MySqlQuery -Database $db -Query $query -Scalar -Credential $mycreds
Assert 10 $result 3


############################################################################################
#test 11, rollback transaction
$query1 = "insert into $table (somestring,someInt,someDateTime) values ('row7',117,'2022-11-27'); "
$query2 = "insert into $table (somestring,someInt,someDateTime) values ('row8',118,'2022-11-28'); "
$query3 = "insert into $table (somestring,someInt,someDateTime) values ('row9',119,'2022-11-29'); "
Start-Transaction -Verbose
$result = ( Use-Transaction -TransactedScript { (($query1, $query2, $query3) | Invoke-MySqlQuery -Database $db -Credential $mycreds -NonQuery -WhatIf -UseTransaction -Verbose) } -UseTransaction)
Undo-Transaction -Verbose
Assert 11 $result.Count  3
$query = "select count(*) as count from $table where someInt in (117,118,119);"
$result = Invoke-MySqlQuery -Query $query -Database $db -Scalar -Credential $mycreds
Assert 11 $result 0


############################################################################################
#test 12, rollback transaction due to unique key error
$query1 = "insert into $table (somestring,someInt,someDateTime) values ('row7',120,'2022-11-27'); "
$query2 = "insert into $table (somestring,someInt,someDateTime) values ('row8',121,'2022-11-28'); "
$query3 = "insert into $table (somestring,someInt,someDateTime) values ('row9',121,'2022-11-29'); "
$ErrorActionPreference = "Continue"
Try {
 Start-Transaction -Verbose
 $rowCount = (Use-Transaction -TransactedScript { $query1 | Invoke-MySqlQuery -Database $db -NonQuery -UseTransaction -Verbose -Credential $mycreds} -UseTransaction)
 Assert 12 $rowCount 1
 $rowCount = (Use-Transaction -TransactedScript { $query2 | Invoke-MySqlQuery -Database $db -NonQuery -UseTransaction -Verbose -Credential $mycreds} -UseTransaction)
 Assert 12 $rowCount 1
 Write-Output "The following error is expected....."
 $rowCount = (Use-Transaction -TransactedScript { $query3 | Invoke-MySqlQuery -Database $db -NonQuery -UseTransaction -Verbose -Credential $mycreds} -UseTransaction)
 Assert 12 $rowCount 
 Complete-Transaction -Verbose  ##this is run even in the try catch! What do I set $ErrorActionPreference to in order to fall into the catch when an error occurs?
}
Catch [system.exception] { 
    Write-Output "Caught exception..."
}
Finally {
 $query = "select count(*) as count from $table where someInt in (120,121);"
 $result = Invoke-MySqlQuery -Query $query -Database $db -Scalar -Verbose -Credential $mycreds
 Assert 12 $result 0
}

############################################################################################
#test 13, WhatIf
$query1 = "insert into $table (somestring,someInt,someDateTime) values ('row7',125,'2022-11-27'); "
$query2 = "insert into $table (somestring,someInt,someDateTime) values ('row8',126,'2022-11-28'); "
$query3 = "insert into $table (somestring,someInt,someDateTime) values ('row9',127,'2022-11-29'); "
Start-Transaction -Verbose
$result = ( Use-Transaction -TransactedScript { (($query1, $query2, $query3) | Invoke-MySqlQuery -Database $db -Credential $mycreds -NonQuery -WhatIf -UseTransaction -Verbose) } -UseTransaction)
Complete-Transaction -Verbose
Assert 13 $result.Count  3
Assert 13 $result[0] 0
Assert 13 $result[1] 0
Assert 13 $result[2] 0

