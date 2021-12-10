use master;
go

-- delete db if exists
if  db_id('TickerTracker') is not null
  set noexec on; -- don't execute rest of commands
go

-- create db
create database TickerTracker;
go

-- switch to db
use TickerTracker;

-- create tables

CREATE TABLE Users (
  Id BIGINT PRIMARY KEY check(Id > 0),
  Handle VARCHAR(30) NOT NULL,
  Name VARCHAR(100) NOT NULL,
  Avatar VARCHAR(300) NOT NULL,
  Token VARCHAR(150),
  Secret VARCHAR(150),
  SessionId VARCHAR(32),
  unique ( Id )
);

CREATE TABLE Options (
  Name VARCHAR(100) PRIMARY KEY NOT NULL,
  Value NVARCHAR(max) NOT NULL,
  Updated DATETIME NOT NULL,
  unique ( Name )
);
