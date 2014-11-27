
DROP DATABASE InvokeQueryTest;
DROP USER 'InvokeSqlLogin'@'localhost';

Create database InvokeQueryTest;

CREATE USER 'InvokeSqlLogin'@'localhost' IDENTIFIED BY '92847rivopwkxur';

GRANT ALL PRIVILEGES ON *.* TO 'InvokeSqlLogin'@'localhost';

create table `invokequerytest`.`TestTable1` (
   `id` int(11) NOT NULL AUTO_INCREMENT,
   `somestring` varchar(255) NOT NULL, 
   `someInt` int(11) NOT NULL,
   `someDateTime` datetime NOT NULL,
   PRIMARY KEY (`id`),
   UNIQUE KEY (`someInt`)
);


insert into `invokequerytest`.`TestTable1` (`somestring`,`someInt`,`someDateTime`)
	values ('blahblah',1,'2014-11-20'),('haha',2,'2014-11-21'),('yaya',3,'2014-11-22');
	
	