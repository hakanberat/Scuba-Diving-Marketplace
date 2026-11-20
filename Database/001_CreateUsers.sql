USE DiveMarketplaceDb;
IF OBJECT_ID('dbo.AppUsers','U') IS NULL
CREATE TABLE dbo.AppUsers(Id int IDENTITY PRIMARY KEY, FirstName nvarchar(100) NOT NULL, LastName nvarchar(100) NOT NULL,
Email nvarchar(200) NOT NULL UNIQUE, PasswordHash nvarchar(1000) NOT NULL, Role nvarchar(20) NOT NULL CHECK(Role IN ('Admin','DiveCenter','Customer')),
Phone nvarchar(50) NULL, IsActive bit NOT NULL DEFAULT 1, CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME());