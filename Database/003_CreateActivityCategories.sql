USE DiveMarketplaceDb;
IF OBJECT_ID('dbo.ActivityCategories','U') IS NULL
CREATE TABLE dbo.ActivityCategories(Id int IDENTITY PRIMARY KEY, Name nvarchar(100) NOT NULL, Slug nvarchar(100) NOT NULL UNIQUE, IsActive bit NOT NULL DEFAULT 1);