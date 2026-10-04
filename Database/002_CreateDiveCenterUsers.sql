USE DiveMarketplaceDb;
IF OBJECT_ID('dbo.DiveCenterUsers','U') IS NULL
CREATE TABLE dbo.DiveCenterUsers(Id int IDENTITY PRIMARY KEY, DiveCenterId int NOT NULL REFERENCES dbo.DiveCenters(Id),
AppUserId int NOT NULL REFERENCES dbo.AppUsers(Id), IsOwner bit NOT NULL DEFAULT 1, CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), UNIQUE(DiveCenterId,AppUserId));