USE DiveMarketplaceDb;
IF OBJECT_ID('dbo.Instructors','U') IS NULL
CREATE TABLE dbo.Instructors(Id int IDENTITY PRIMARY KEY, DiveCenterId int NOT NULL REFERENCES dbo.DiveCenters(Id),
FirstName nvarchar(100) NOT NULL, LastName nvarchar(100) NOT NULL, Bio nvarchar(max) NULL, PhotoUrl nvarchar(500) NULL,
CertificationAgency nvarchar(50) NULL, InstructorNumber nvarchar(100) NULL, IsActive bit NOT NULL DEFAULT 1, CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME());