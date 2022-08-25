# InvokeQuery
## A powershell module for querying databases.

Currently supported database types:
* Sql Server
* MySql
* ODBC
* PostgreSql
* Sql Server CE
* SqLite
* Firebird
* Oracle (requires separate driver download.)

It's trivial to add a new db type as long as it has an ADO.NET provider. Please submit a pull request or open an issue if you have one you'd like to add.

### Why not use Invoke-Sqlcmd??
Invoke-Sqlcmd is just a wrapper for Sql Server's command-line executable, Sqlcmd.exe:
```
PS C:\windows\system32> Invoke-Sqlcmd

PS SQLSERVER:\> 
```
Many admins are accustomed to using Sqlcmd; it's great at running DDL, however I think it's terrible at interacting with data.
The Cmdlets in the InvokeQuery module are all built on ADO.NET, and therefore works great at querying and manipulating data.
We also have functionality that's not possible using Sqlcmd, like scalar queries, returning the number of rows affected in a CUD operation, and ...oh yeah... **transactions!** It's also trivial to make the same codebase work with any DB type that has an ADO.NET provider.

### How to install:
If you have powershell v5 you can install directly from the [Powershell Gallery.](https://www.powershellgallery.com/packages/InvokeQuery)
```
Install-Module -Name InvokeQuery
```

If you are on pre-v5 powershell, [download the latest release](https://github.com/ctigeek/InvokeQueryPowershellModule/releases) and unzip to `C:\Windows\System32\WindowsPowerShell\v1.0\Modules\InvokeQuery`.


### Usage:
All examples use Sql Server. Here's the definition of the table that I'm using in all examples:
```sql
use [test]
GO
CREATE TABLE [dbo].[table1](
  [pk] [uniqueidentifier] NOT NULL,
  [someint] [bigint] NOT NULL,
  [somestring] [nvarchar](500) NOT NULL,
  [somedatetime] [datetime2](7) NOT NULL,
  CONSTRAINT [PK_table1] PRIMARY KEY CLUSTERED ([pk] ASC)
)
GO
--Seed data
insert into table1 values (NEWID(), 123, 'blah blah blah', GETDATE())
insert into table1 values (NEWID(), 321, 'yo yo yo', GETDATE())
insert into table1 values (NEWID(), 213, 'hey hey hey', GETDATE())
```
### Connect Datase

PS C:\windows\system32> $db = "test"
| Invoke-MySqlQuery -ConnectionString "Server=localhost;Uid=root;Pwd=pwdatabase;database=namedatabase;" 

```
### Querying Data

Let's see how to do a simple query and then discuss:
```
PS C:\windows\system32> $db = "test"
PS C:\windows\system32> $sql = "select * from table1;"
PS C:\windows\system32> $results = Invoke-SqlServerQuery -Sql $sql -Database $db
PS C:\windows\system32> $results.Count
3
PS C:\windows\system32> $results | ft

pk                                   someint somestring     somedatetime       
--                                   ------- ----------     ------------       
d749777e-bdf0-43e1-901d-6874785e4c71     321 yo yo yo       5/1/2016 8:58:02 AM
63e58168-581d-4c28-9fb7-6b4b76c5c554     213 hey hey hey    5/1/2016 8:58:02 AM
0867b9cd-712e-4181-b739-b2f16cff520c     123 blah blah blah 5/1/2016 8:58:02 AM

```
As you can see, making queries is incredibly easy. Once you have the results, you can mainpulate it in powershell like any other object.  Because we are connecting to a local database using windows authentication, we don't need to specify additional parameters; the `Server` property defaults to `localhost`, and the `Credential` property defaults to windows authentication.  If you use the `Verbose` switch you can see all this happening:
```
PS C:\windows\system32> $results = $sql | Invoke-SqlServerQuery -Database $db -Verbose
VERBOSE: Server set to localhost
VERBOSE: Using the following connection string: Data Source=localhost;Initial Catalog=test;Integrated Security=SSPI;
VERBOSE: Opening connection...
VERBOSE: Connection to database is open.
VERBOSE: Running query number 1
VERBOSE: Running the following query: select * from table1;
VERBOSE: Performing the operation "Run Query:`select * from table1;`" on target "Database server".
VERBOSE: Query returned 3 rows.
VERBOSE: Complete...
VERBOSE: Processed 1 queries in 11 milliseconds.
```
Not only did it state that it was using `localhost`, but it gives you the full connection string it will use to connect to the database.  You can actually pass in a full connection string using the `ConnectionString` parameter if you need to use special db properties.  You may also notice that this time we piped in the `$sql` variable instead of passing it as a parameter. We'll see how to make good use of that feature in a bit.

### Scalar queries
Need a single value? No problem. Use the `Scalar` switch. It will return a single value.
```
PS C:\windows\system32> $db = "test"
PS C:\windows\system32> $sql = "select somestring from table1 where someint = 321;"
PS C:\windows\system32> $somestring = $sql | Invoke-SqlServerQuery -Database $db -Scalar
PS C:\windows\system32> $somestring
yo yo yo
```
### CUD Operations
Performing Create/Update/Delete operations is really where things get fun, and InvokeQuery has some wonderful features to make your life easy. A simple example:
```
PS C:\windows\system32> $db = "test"
PS C:\windows\system32> $guid = New-Guid
PS C:\windows\system32> $sql = "insert into table1 values ('$guid', 432, 'powershell is awesome', getdate());"
PS C:\windows\system32> $rowcount = $sql | Invoke-SqlServerQuery -Database $db -CUD
PS C:\windows\system32> $rowcount
1
```
All we did was add the `CUD` switch. That tells the module to execute a "non-query" operation, i.e. this is a query that will not return data. When you use the CUD switch, it returns the number of rows created/updated/deleted. 

_Sidebar 1: this actually breaks a tenant that a cmdlet should behave consistently. i.e. if we are querying data, it returns data, but with the `CUD` switch, it's actually returning meta-data: e.g. row count. If this offends you, sorry. The alternative is to create a whole new cmdlet just for CUD operations (e.g. Invoke-SqlServerCUD) and I think that's going overboard._

_Sidebar 2: If you run a SQL operation with `CREATE`, `UPDATE`, or `DELETE`, but do not include the `CUD` switch, you'll be prompted to confirm the operation. The only way to avoid the nag confirmation is to use the `CUD` switch._

### Parameters

Okay, so that's all very simple, but we are using string concatenation to insert the guid into the sql statement. Let's look at a more problematic example:
```
PS C:\windows\system32> $guid = New-Guid
PS C:\windows\system32> $somestring = "It's great to see you!"
PS C:\windows\system32> $somestring
It's great to see you!
PS C:\windows\system32> $sql = "insert into table1 values ('$guid', 432, '$somestring', getdate());"
PS C:\windows\system32> $sql
insert into table1 values ('722dbaf2-5132-479e-80bb-62e88815e474', 432, 'It's great to see you!', getdate());
```
Do you see the problem with that insert statement? The single-quote from `$somestring` isn't escaped. If we try to execute this, Sql Server will throw an error. (Not to mention the possible sql injection vulnerabilities!)  Generally speaking, using string concatenation to build a sql statement is considered very poor practice, even if you build your own escaping functionality, so let's not do it. Instead we can use the `Parameters` parameter to pass in a hash:
```
PS C:\windows\system32> $params = @{"pk"=$guid; "someint"=444; "somestring"=$somestring}
PS C:\windows\system32> $params

Name                           Value                                                             
----                           ----- 
somestring                     It's great to see you!
pk                             722dbaf2-5132-479e-80bb-62e88815e474
someint                        444

PS C:\windows\system32> $sql = "insert into table1 values (@pk, @someint ,@somestring, getdate());"
PS C:\windows\system32> $rowcount = $sql | Invoke-SqlServerQuery -Database $db -Parameters $params -CUD
PS C:\windows\system32> $rowcount
1
PS C:\windows\system32> $sql = "select somestring from table1 where pk = @pk;"
PS C:\windows\system32> $sql | Invoke-SqlServerQuery -Database $db -Parameters $params -Scalar
It's great to see you!
```

### Transactions
There are two ways to utilize transactions with this module:
1. Explicitly creating one via `Start-Transaction`, and then using the `UseTransaction` switch on the cmdlet.
2. Piping in multiple queries and letting the cmdlet create a transaction for you.

Let's actually start with the latter: piping in multiple queries, using the `Verbose` switch, and see what happens.
```
PS C:\windows\system32> 1..4 | % {
    $num = $_
    $sql = "insert into table1 values (NEWID(), $num, 'testy mctestface $num',GETDATE())"
    $sql
} | Invoke-SqlServerQuery -Database $db -CUD -Verbose

VERBOSE: Server set to localhost
VERBOSE: Using the following connection string: Data Source=localhost;Initial Catalog=test;Integrated Security=SSPI;
VERBOSE: Opening connection...
VERBOSE: Connection to database is open.
VERBOSE: Running query number 1
VERBOSE: Running the following query: insert into table1 values (NEWID(), 1, 'testy mctestface 1',GETDATE())
VERBOSE: Performing the operation "Run CUD Query:`insert into table1 values (NEWID(), 1, 'testy mctestface 1',GETDATE())`" on target "Database server"
.
VERBOSE: Starting transaction.
VERBOSE: CUD query complete. 1 rows affected.
1
VERBOSE: Running query number 2
VERBOSE: Running the following query: insert into table1 values (NEWID(), 2, 'testy mctestface 2',GETDATE())
VERBOSE: Performing the operation "Run CUD Query:`insert into table1 values (NEWID(), 2, 'testy mctestface 2',GETDATE())`" on target "Database server"
.
VERBOSE: CUD query complete. 1 rows affected.
1
VERBOSE: Running query number 3
VERBOSE: Running the following query: insert into table1 values (NEWID(), 3, 'testy mctestface 3',GETDATE())
VERBOSE: Performing the operation "Run CUD Query:`insert into table1 values (NEWID(), 3, 'testy mctestface 3',GETDATE())`" on target "Database server"
.
VERBOSE: CUD query complete. 1 rows affected.
1
VERBOSE: Running query number 4
VERBOSE: Running the following query: insert into table1 values (NEWID(), 4, 'testy mctestface 4',GETDATE())
VERBOSE: Performing the operation "Run CUD Query:`insert into table1 values (NEWID(), 4, 'testy mctestface 4',GETDATE())`" on target "Database server"
.
VERBOSE: CUD query complete. 1 rows affected.
1
VERBOSE: Committing transaction.
VERBOSE: Closing connection.
VERBOSE: Processed 4 queries in 99 milliseconds.
VERBOSE: Complete.
```
That's a lot of verbose output, but most of it is just telling you what query is about to be executed. However there are two very important things to note:
* If you pipe multiple queries into the cmdlet, it uses the same connection for every query. Notice towards the top it says `Opening connection.`, and at the bottom there's `Closing connection.`.
* If you aren't using the `UseTransaction` switch (i.e. if a transaction doesn't already exist), it will create a transaction and all queries will execute within the scope of that transaction. Again, in the verbose output, there's `Starting transaction.` just before the first query is executed, and then `Committing transaction.` at the end.

These are really important points: if you pipe in 5 queries and the last one throws an error, the entire transaction will be rolled back, leaving the database in the state it was in before the first 4 queries were executed. If you **don't** want multiple piped queries to run within a transaction, then you can use the `NoTrans` switch and each query piped in will run outside of a transaction, but still within the same connection.

#### SqlQuery parameter
Cool! Now we are starting to see the power we have with this module. You'll notice that in the query above we went back to using string concatenation to build our queries. We couldn't pass in a `Parameters` hash object since the value of `someint` would be different for each query. We could pass in multiple named parameters, but that would get messy real fast. Instead the module includes an object you can create to pass in called `SqlQuery`. You can build your own SqlQuery object, but I recommend using the helper cmdlet: `New-SqlQuery`. Let's rewrite that previous example:
```Powershell
PS C:\windows\system32> $newGuids = {}.Invoke()
PS C:\windows\system32> 1..4 | % {
    $num = $_
    $guid = New-Guid
    $newGuids.Add($guid)
    $params = @{ "pk"=$guid; "someint"=$num; "somestring"="testy mctestface $num.";}
    $sql = "insert into table1 values (@pk, @someint, @somestring, GETDATE())"
    New-SqlQuery -Sql $sql -Parameters $params -CUD
} | Invoke-SqlServerQuery -Database $db

1
1
1
1

PS C:\windows\system32> $newGuids

Guid                                
----                                
9d956308-7ded-4e45-8f27-b2e23f3a08ed
baea0b61-1ddd-49fa-86b4-58d0a2bcc1c4
b2f3ff89-1f10-4746-9021-3f64ef6946ee
8684f854-20c0-4e0f-a0e7-8602cfcbb0e0
```
So for each iteration, it creates a new SqlQuery object and that object is piped into the Invoke-SqlServerQuery cmdlet. (I didn't use the `Verbose` switch here, but the output would look identical as above but with a few extra lines for the parameters.) I also saved all the GUIDs for later processing. It's worth noting that the `Database` parameter is still used with the cmdlet and not part of the SqlQuery object. This is because it's used to build the connection string, and once the connection is open the target database cannot change. If you need to target multiple databases on a server, you can always explicitly specify the database in the query itself. 

#### ExpectedRowCount parameter
Consider this scenario: you want to update a single row in a very critical and busy database table. Update and Delete statements are inherently dangerous: one little mistake in your WHERE clause and you're impacting a lot more rows than intended. The `ExpectedRowCount` parameter ensures that you only impact the number of rows intended. If the number of rows changed by a CUD operation doesn't match the parameter, the transaction is rolled back.  This is especially helpful if you have a number of queries you're piping in, since it won't proceed to the next query in the pipe if the current query's row count doesn't match the `ExpectedRowCount` parameter.

```
PS C:\windows\system32> 1..4 | % {
    $num = $_
    $guid = New-Guid
    $params = @{ "pk"=$guid; "someint"=$num; "somestring"="testy mctestface $num.";}
    $sql = "insert into table1 values (@pk, @someint, @somestring, GETDATE())"
    New-SqlQuery -Sql $sql -Parameters $params -ExpectedRowCount $num -CUD
} | Invoke-SqlServerQuery -Database $db -Verbose

VERBOSE: Server set to localhost
VERBOSE: Using the following connection string: Data Source=localhost;Initial Catalog=test;Integrated Security=SSPI;
VERBOSE: Opening connection.
VERBOSE: Connection to database is open.
VERBOSE: Running query number 1
VERBOSE: Running the following query: insert into table1 values (@pk, @someint, @somestring, GETDATE())
VERBOSE: Performing the operation "Run CUD Query:`insert into table1 values (@pk, @someint, @somestring, GETDATE())`" on target "Database server".
VERBOSE: Adding parameter somestring=testy mctestface 1.
VERBOSE: Adding parameter pk=262b25db-e9f7-4285-9c95-1a1e27e06520
VERBOSE: Adding parameter someint=1
VERBOSE: Starting transaction.
VERBOSE: CUD query complete. 1 rows affected.
1
VERBOSE: Running query number 2
VERBOSE: Running the following query: insert into table1 values (@pk, @someint, @somestring, GETDATE())
VERBOSE: Performing the operation "Run CUD Query:`insert into table1 values (@pk, @someint, @somestring, GETDATE())`" on target "Database server".
VERBOSE: Adding parameter somestring=testy mctestface 2.
VERBOSE: Adding parameter pk=9b0c1842-74c5-47bd-8c04-938f8eb360f4
VERBOSE: Adding parameter someint=2
WARNING: Rolling back transaction!
VERBOSE: Closing connection.
VERBOSE: Processed 2 queries in 77 milliseconds.
VERBOSE: Complete.
Invoke-SqlServerQuery : The ExpectedRowCount is 2, but this query had a row count of 1 rows. Rolling back the transaction.
At line:7 char:5
+ } | Invoke-SqlServerQuery -Database $db -Verbose
+     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    + CategoryInfo          : InvalidOperation: (:) [Invoke-SqlServerQuery], PSInvalidOperationException
    + FullyQualifiedErrorId : InvalidOperation,InvokeQuery.InvokeSqlServerQuery
 
 
```
This is the same query as the previous example, however I used the $num variable, which increments on each iteration, as the value for the `ExpectedRowCount` parameter. For the first query the actual rows affected and the expected row count would match, but for the second query they would not. After the second query the transaction is rolled back.  

#### Using Powershell transactions.
If you want to run multiple cmdlets within the same transaction, you can create your own transaction scope.

**Important**: Creating and running distributed transactions, i.e. transactions that involve multiple instances of Sql Server or other types of databases, can get complicated. e.g. The instances of Sql Server probably need to be able to communicate directly, and may need certain domain-level permissions. Needless to say, that discussion is outside the bounds of this readme. I can say with certainty that Sql Server and MySql cannot share a distributed transaction.

```
PS C:\windows\system32> try {
    $db = "test"
    Start-Transaction
    $sql = "insert into table1 values (NEWID(), 8765, 'transactions!', GETDATE())"
    $result = $sql | Invoke-SqlServerQuery -Database $db -UseTransaction -CUD -Verbose
    $result

    $sql = "insert into table1 values (NEWID(), 5555, 'transaction too!', GETDATE())"
    $result = $sql | Invoke-SqlServerQuery -Database $db -UseTransaction -CUD -Verbose
    $result
    Complete-Transaction
}
catch {
    ##Transaction will automatically be rolled back....
    Write-Error $_
}

VERBOSE: Server set to localhost
VERBOSE: Using the following connection string: Data Source=localhost;Initial Catalog=test;Integrated Security=SSPI;
VERBOSE: Opening connection.
VERBOSE: Creating DB connection in established transaction scope.
VERBOSE: Connection to database is open.
VERBOSE: Running query number 1
VERBOSE: Running the following query: insert into table1 values (NEWID(), 8765, 'transactions!', GETDATE())
VERBOSE: Performing the operation "Run CUD Query:`insert into table1 values (NEWID(), 8765, 'transactions!', GETDATE())`" on target "Database server".
VERBOSE: CUD query complete. 1 rows affected.
VERBOSE: Closing connection.
VERBOSE: Processed 1 queries in 18 milliseconds.
VERBOSE: Complete.
1
VERBOSE: Server set to localhost
VERBOSE: Using the following connection string: Data Source=localhost;Initial Catalog=test;Integrated Security=SSPI;
VERBOSE: Opening connection.
VERBOSE: Creating DB connection in established transaction scope.
VERBOSE: Connection to database is open.
VERBOSE: Running query number 1
VERBOSE: Running the following query: insert into table1 values (NEWID(), 5555, 'transaction too!', GETDATE())
VERBOSE: Performing the operation "Run CUD Query:`insert into table1 values (NEWID(), 5555, 'transaction too!', GETDATE())`" on target "Database server".
VERBOSE: CUD query complete. 1 rows affected.
VERBOSE: Closing connection.
VERBOSE: Processed 1 queries in 4127 milliseconds.
VERBOSE: Complete.
1
```

1. First we create a transaction using `Start-Transaction`.
2. Then we simply include the `UseTransaction` switch in each call to the cmdlet.
3. We commit the transaction by calling `Complete-Transaction`.

Notice in the verbose output for each call is `Creating DB connection in established transaction scope.`.  
So it's not creating a transaction. It's using the existing one.
Any error that's thrown prior to the `Complete-Transaction` will cause the transaction to rollback.

**Note**: if you run this code, you will notice a significant delay when the second cmdlet is executed. This is due to the transaction being moved from Sql Server to the Microsoft Distributed Transaction Coordinator service. Subsequent calls to the cmdlet should not have such a delay.

### Callback parameter
The callback parameter allows you to run code between queries, even when they're part of a transaction. There are two good use-cases for callback: retrieving identity and other values generated by the database server, and complex validation processes that may require rolling back a transaction.
For this example, we'll be using a table with identity:
```
use test
GO
CREATE TABLE [dbo].[table2](
	[pk] [bigint] IDENTITY(1,1) NOT NULL,
	[someint] [bigint] NOT NULL,
	[somestring] [varchar](5000) NOT NULL,
	[somedatetime] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_table2] PRIMARY KEY CLUSTERED ([pk] ASC)
)
GO
```

This code will insert three rows, and put the resulting identity values into an array:

```
$db = "test"
$pks = {}.Invoke();
$callback = {
   param($sqlquery, $result)
   $pks.Add($result.pk)
}

5..8 | %{ 
    $sql = "insert into table2 (someint, somestring, somedatetime) values ($_, 'blah blah blah', GETDATE())"
    New-SqlQuery -Sql $sql -CUD
    New-SqlQuery -Sql "select @@IDENTITY as pk" -Callback $callback
} | Invoke-SqlServerQuery -Database $db

```
So all callbacks are passed two parameters: The `SqlQuery` object that was executed and the result of the query. If it's a CUD query, then result is the row count, otherwise it's the data returned from the query.


## Options for all Invoke-*Query cmdlets:

#### Options related to the connection:
* `Credential` _(PSCredential)_ - The user ID and password used to build the connection string. If not provided, it defaults to windows authentication.
* `Server` _(string)_ - The name of the server used to build the connection string. If not provided, it defaults to "localhost". For Sql Server it defaults to "localhost" plus the name of the default instance of the database.
* `Database` _(string)_ - The name of the database used to build the connection string. No database name-value pair is added to the connection string if empty.
* `ConnectionTimeout` _(integer)_ - The connection timeout to use in the connection string. No default value.
* `ConnectionString` _(string)_ - The full connection string. If you specify this, do not use the Credential, Server, Database, or ConnectionTimeout options.
* `UseTransaction` _(switch)_ - Use an existing ambiant transaction, created with the Start-Transaction command.
* `NoTrans` _(switch)_ - Use with `CUD`. Do not use with `UseTransaction` or `ExpectedRowCount`. Indicates that the CUD operation(s) should NOT be run within a transaction. This applies to all queries piped into a single cmdlet.

#### Options related to the command:
* `Sql` _(string, pipeline)_ - The SQL statement to be executed. This can be raw SQL, or the name of a stored procedure if you use the `StoredProcedure` switch.
* `Parameters` - _(hashtable)_ - The parameters to pass with the SQL command.
* `CommandTimeout` _(integer)_ - The command timeout in seconds.
* `CUD` _(switch)_ - Indicates that the SQL statement contains a Create, Update, or Delete statement. It will execute the query using the ADO.NET command ExecuteNonQuery method and return the number of rows affected.
* `Scalar` _(switch)_ - Indicates that this SQL statement should only return the first column of the first row returned. i.e. using this option will always return a single value or null.
* `ExpectedRowCount` _(integer)_ - Use with `CUD`. Indicates the number or rows expected to be affected by the operation. If the number of actual affected rows does not match this parameter, the transaction is rolled back and an error is thrown.
* `SqlQuery` _(SqlQuery object)_ - Use this object to encapsulate any command parameters so they can be piped into the cmdlet. You can create a SqlQuery object via the New-SqlQuery cmdlet. If you use this parameter you should not use any of the other command options. See examples for details.

#### Other standard options:
* `WhatIf` - Don't actually run the query, but show what would happen.
* `Confirm` - Confirm high-impact operations.
* `Verbose` - Display lots of logging. This will show you the connection string built, the Sql command being executed, and the parameters that are added.
