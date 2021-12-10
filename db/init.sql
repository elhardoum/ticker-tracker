use master;
go

-- create db if not exists
if db_id('TickerTracker') is null
  create database TickerTracker;
go

-- switch to db
use TickerTracker;

-- create tables

if not exists (select * from sysobjects where name='Users' and xtype='U')
  create table Users (
    Id bigint primary key,
    Handle varchar(30) not null,
    Name varchar(100) not null,
    Avatar varchar(300) not null,
    Token varchar(150),
    Secret varchar(150),
    SessionDd varchar(32)
  )
go

if not exists (select * from sysobjects where name='Options' and xtype='U')
  create table Options (
    Name varchar(100) primary key not null,
    Value nvarchar(max) not null,
    Updated datetime not null,
    unique ( Name )
  )
go

if not exists (select * from sysobjects where name='Portfolio' and xtype='U')
  create table Portfolio (
    Id bigint primary key identity,
    UserId bigint not null references Users (Id),
    Symbol varchar(100) not null,
    IsCrypto int default 0 check (IsCrypto in (0, 1)),
    Enabled int default 1 check (Enabled in (0, 1)),
    [Percent] decimal(5,2) not null check ( [Percent] <> 0 and [Percent] <= 100 ),
    TweetText varchar(500),
    Updated datetime not null,
  )
go
