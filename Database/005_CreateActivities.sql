USE DiveMarketplaceDb;
IF OBJECT_ID('dbo.Activities','U') IS NULL
CREATE TABLE dbo.Activities(Id int IDENTITY PRIMARY KEY, DiveCenterId int NOT NULL REFERENCES dbo.DiveCenters(Id), ActivityCategoryId int NOT NULL REFERENCES dbo.ActivityCategories(Id),
Name nvarchar(200) NOT NULL, ShortDescription nvarchar(500) NULL, Description nvarchar(max) NULL, Country nvarchar(100) NULL, City nvarchar(100) NULL,
Location nvarchar(500) NULL, Price decimal(18,2) NOT NULL CHECK(Price>=0), CurrencyCode char(3) NOT NULL CHECK(CurrencyCode IN ('TRY','USD','EUR','GBP')),
Duration nvarchar(100) NULL, MinimumAge int NULL CHECK(MinimumAge>=0), MinimumCertification nvarchar(200) NULL, MaxParticipants int NULL CHECK(MaxParticipants>0),
Included nvarchar(max) NULL, NotIncluded nvarchar(max) NULL, Requirements nvarchar(max) NULL, MainImageUrl nvarchar(500) NULL, IsActive bit NOT NULL DEFAULT 1,
CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2 NULL, CONSTRAINT UQ_Activities_Center UNIQUE(Id,DiveCenterId));