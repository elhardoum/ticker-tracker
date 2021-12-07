use master;
go

-- delete db if exists
if  db_id('TickerTracker') is not null
  --drop database TickerTracker;
  set noexec on; -- don't execute rest of commands
go

-- create db
create database TickerTracker;
go

-- switch to db
use TickerTracker;

-- create tables

CREATE TABLE Users (
  Id INT PRIMARY KEY check(Id > 0),
  Handle VARCHAR(30) NOT NULL,
  Name VARCHAR(100) NOT NULL,
  Avatar VARCHAR(300) NOT NULL,
  Token VARCHAR(150),
  Secret VARCHAR(150),
  unique ( Id )
);
