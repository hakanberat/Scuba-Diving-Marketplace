USE DiveMarketplaceDb;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
IF OBJECT_ID('dbo.DiveCenterApplications','U') IS NULL
CREATE TABLE dbo.DiveCenterApplications(
 Id int IDENTITY PRIMARY KEY,
 AppUserId int NOT NULL UNIQUE REFERENCES dbo.AppUsers(Id),
 Name nvarchar(200) NOT NULL, Description nvarchar(max) NULL,
 Country nvarchar(100) NOT NULL, City nvarchar(100) NOT NULL,
 Address nvarchar(500) NULL, Phone nvarchar(50) NULL, Website nvarchar(300) NULL,
 Status nvarchar(20) NOT NULL DEFAULT 'Pending' CHECK(Status IN('Pending','Approved','Rejected')),
 ReviewNotes nvarchar(2000) NULL, DiveCenterId int NULL REFERENCES dbo.DiveCenters(Id),
 CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), ReviewedAt datetime2 NULL,
 ReviewedByUserId int NULL REFERENCES dbo.AppUsers(Id));
IF OBJECT_ID('dbo.Notifications','U') IS NULL
BEGIN
 CREATE TABLE dbo.Notifications(
  Id int IDENTITY PRIMARY KEY, AppUserId int NOT NULL REFERENCES dbo.AppUsers(Id),
  EventKey nvarchar(100) NOT NULL, Title nvarchar(200) NOT NULL,
  Message nvarchar(1000) NOT NULL, LinkUrl nvarchar(300) NOT NULL,
  IsRead bit NOT NULL DEFAULT 0, CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT UQ_Notifications_Event UNIQUE(AppUserId,EventKey));
 CREATE INDEX IX_Notifications_User ON dbo.Notifications(AppUserId,IsRead,CreatedAt);
END;
