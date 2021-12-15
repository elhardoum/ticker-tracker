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
    SessionId varchar(32)
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
    LastTweetedQuoteTimestamp datetime,
  )
go

if not exists (select * from sysobjects where name='Tweets' and xtype='U')
  create table Tweets (
    Id bigint primary key identity,
    [Text] varchar(500) not null,
    [Url] varchar(200),
    PortfolioId bigint not null references Portfolio (Id),
    Created datetime not null,
  )
go

if not exists (select * from sysobjects where name='Quotes' and xtype='U')
  create table Quotes (
    Id bigint primary key identity,
    Symbol varchar(100) not null,
    Quote decimal(18,2) not null,
    Updated datetime not null,
    LastQuote decimal(18,2),
    LastQuoteUpdated datetime,
  )
go

if not exists (select * from sysobjects where name='DictQuotes' and xtype='U')
  create table DictQuotes (
    Symbol varchar(100) primary key not null,
    Movement decimal(10,2) not null check(Movement > 0 or Movement < 0),
    LastQuote decimal(18,2) not null,
    Quote decimal(18,2) not null,
    Updated datetime not null,
    unique(Symbol)
  )
go
