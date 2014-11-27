------------------------- drop stuff
USE [master]
GO
DROP DATABASE [InvokeQueryTest]
GO
Drop LOGIN [InvokeSqlLogin]
GO
---------------------------create stuff
USE MASTER
GO
create database InvokeQueryTest;
GO
CREATE LOGIN [InvokeSqlLogin] WITH PASSWORD=N'92847rivopwkxur', DEFAULT_DATABASE=[InvokeQueryTest], DEFAULT_LANGUAGE=[us_english], CHECK_EXPIRATION=OFF, CHECK_POLICY=OFF
GO

USE [InvokeQueryTest]
GO
CREATE USER [InvokeQueryUser] FOR LOGIN [InvokeSqlLogin] WITH DEFAULT_SCHEMA=[dbo]
GO
EXEC sp_addrolemember N'db_owner', N'InvokeQueryUser'

--------------------------- create table and proc
use InvokeQueryTest;
create table TestTable1 (
   [id] [int] IDENTITY(1,1) NOT NULL,
   [somestring] [nvarchar](200) NOT NULL,
   [someInt] [int] NOT NULL,
   [someDateTime] [datetime] NOT NULL,
   PRIMARY KEY CLUSTERED ([id] ASC)
);
GO
CREATE UNIQUE NONCLUSTERED INDEX [NonClusteredIndex-20141127-110617] ON [dbo].[TestTable1] ([someInt] ASC);
GO




use InvokeQueryTest;
GO
CREATE PROCEDURE [dbo].[TestProc1]
AS
BEGIN
	SET NOCOUNT ON;
	select * from dbo.TestTable1;
END
GO

use InvokeQueryTest;
insert into TestTable1 (somestring,someInt,someDateTime)
	values ('blahblah',1,'2014-11-20'),('haha',2,'2014-11-21'),('yaya',3,'2014-11-22');

GO





