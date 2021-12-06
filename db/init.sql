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
  id INT PRIMARY KEY IDENTITY,
  name VARCHAR(100) NOT NULL
);
