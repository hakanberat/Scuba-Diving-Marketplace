USE master;
GO

IF DB_ID(N'DiveMarketplaceDb') IS NULL
    CREATE DATABASE DiveMarketplaceDb;
GO

USE DiveMarketplaceDb;
GO

IF OBJECT_ID(N'dbo.DiveCenters', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DiveCenters (
        Id int IDENTITY(1,1) PRIMARY KEY,
        Name nvarchar(200) NOT NULL,
        Description nvarchar(max) NULL,
        Country nvarchar(100) NULL,
        City nvarchar(100) NULL,
        Address nvarchar(500) NULL,
        Phone nvarchar(50) NULL,
        Email nvarchar(200) NULL,
        Website nvarchar(500) NULL,
        LogoUrl nvarchar(500) NULL,
        IsActive bit NOT NULL DEFAULT 1,
        CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;
GO

-- 001_CreateUsers.sql
USE DiveMarketplaceDb;
IF OBJECT_ID('dbo.AppUsers','U') IS NULL
CREATE TABLE dbo.AppUsers(Id int IDENTITY PRIMARY KEY, FirstName nvarchar(100) NOT NULL, LastName nvarchar(100) NOT NULL,
Email nvarchar(200) NOT NULL UNIQUE, PasswordHash nvarchar(1000) NOT NULL, Role nvarchar(20) NOT NULL CHECK(Role IN ('Admin','DiveCenter','Customer')),
Phone nvarchar(50) NULL, IsActive bit NOT NULL DEFAULT 1, CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME());
GO


-- 002_CreateDiveCenterUsers.sql
USE DiveMarketplaceDb;
IF OBJECT_ID('dbo.DiveCenterUsers','U') IS NULL
CREATE TABLE dbo.DiveCenterUsers(Id int IDENTITY PRIMARY KEY, DiveCenterId int NOT NULL REFERENCES dbo.DiveCenters(Id),
AppUserId int NOT NULL REFERENCES dbo.AppUsers(Id), IsOwner bit NOT NULL DEFAULT 1, CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), UNIQUE(DiveCenterId,AppUserId));
GO


-- 003_CreateActivityCategories.sql
USE DiveMarketplaceDb;
IF OBJECT_ID('dbo.ActivityCategories','U') IS NULL
CREATE TABLE dbo.ActivityCategories(Id int IDENTITY PRIMARY KEY, Name nvarchar(100) NOT NULL, Slug nvarchar(100) NOT NULL UNIQUE, IsActive bit NOT NULL DEFAULT 1);
GO


-- 004_CreateInstructors.sql
USE DiveMarketplaceDb;
IF OBJECT_ID('dbo.Instructors','U') IS NULL
CREATE TABLE dbo.Instructors(Id int IDENTITY PRIMARY KEY, DiveCenterId int NOT NULL REFERENCES dbo.DiveCenters(Id),
FirstName nvarchar(100) NOT NULL, LastName nvarchar(100) NOT NULL, Bio nvarchar(max) NULL, PhotoUrl nvarchar(500) NULL,
CertificationAgency nvarchar(50) NULL, InstructorNumber nvarchar(100) NULL, IsActive bit NOT NULL DEFAULT 1, CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME());
GO


-- 005_CreateActivities.sql
USE DiveMarketplaceDb;
IF OBJECT_ID('dbo.Activities','U') IS NULL
CREATE TABLE dbo.Activities(Id int IDENTITY PRIMARY KEY, DiveCenterId int NOT NULL REFERENCES dbo.DiveCenters(Id), ActivityCategoryId int NOT NULL REFERENCES dbo.ActivityCategories(Id),
Name nvarchar(200) NOT NULL, ShortDescription nvarchar(500) NULL, Description nvarchar(max) NULL, Country nvarchar(100) NULL, City nvarchar(100) NULL,
Location nvarchar(500) NULL, Price decimal(18,2) NOT NULL CHECK(Price>=0), CurrencyCode char(3) NOT NULL CHECK(CurrencyCode IN ('TRY','USD','EUR','GBP')),
Duration nvarchar(100) NULL, MinimumAge int NULL CHECK(MinimumAge>=0), MinimumCertification nvarchar(200) NULL, MaxParticipants int NULL CHECK(MaxParticipants>0),
Included nvarchar(max) NULL, NotIncluded nvarchar(max) NULL, Requirements nvarchar(max) NULL, MainImageUrl nvarchar(500) NULL, IsActive bit NOT NULL DEFAULT 1,
CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2 NULL, CONSTRAINT UQ_Activities_Center UNIQUE(Id,DiveCenterId));
GO


-- 006_CreateReservations.sql
USE DiveMarketplaceDb;
IF OBJECT_ID('dbo.Reservations','U') IS NULL
CREATE TABLE dbo.Reservations(Id int IDENTITY PRIMARY KEY, ReservationNumber nvarchar(60) NOT NULL UNIQUE,
ActivityId int NOT NULL, DiveCenterId int NOT NULL REFERENCES dbo.DiveCenters(Id), CustomerUserId int NOT NULL REFERENCES dbo.AppUsers(Id),
ReservationDate date NOT NULL, Participants int NOT NULL CHECK(Participants>0), UnitPrice decimal(18,2) NOT NULL CHECK(UnitPrice>=0), CurrencyCode char(3) NOT NULL,
TotalPrice decimal(18,2) NOT NULL CHECK(TotalPrice>=0), Status nvarchar(20) NOT NULL DEFAULT 'Pending' CHECK(Status IN ('Pending','Confirmed','Rejected','Cancelled')),
CustomerNotes nvarchar(2000) NULL, DiveCenterNotes nvarchar(2000) NULL, CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2 NULL,
ConfirmedAt datetime2 NULL, CancelledAt datetime2 NULL, FOREIGN KEY(ActivityId,DiveCenterId) REFERENCES dbo.Activities(Id,DiveCenterId));
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_Reservations_Customer' AND object_id=OBJECT_ID('dbo.Reservations')) CREATE INDEX IX_Reservations_Customer ON dbo.Reservations(CustomerUserId,CreatedAt);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_Reservations_Center' AND object_id=OBJECT_ID('dbo.Reservations')) CREATE INDEX IX_Reservations_Center ON dbo.Reservations(DiveCenterId,Status,ReservationDate);
GO


-- 010_CreatePhotos.sql
USE DiveMarketplaceDb;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
IF OBJECT_ID('dbo.Photos','U') IS NULL
BEGIN
 CREATE TABLE dbo.Photos (
  Id int IDENTITY PRIMARY KEY,
  DiveCenterId int NULL REFERENCES dbo.DiveCenters(Id),
  ActivityId int NULL REFERENCES dbo.Activities(Id),
  ImageUrl nvarchar(500) NOT NULL,
  Caption nvarchar(200) NULL,
  IsCover bit NOT NULL DEFAULT 0,
  CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT CK_Photos_OneParent CHECK(
   (DiveCenterId IS NOT NULL AND ActivityId IS NULL) OR
   (DiveCenterId IS NULL AND ActivityId IS NOT NULL))
 );
END;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Photos') AND name='IX_Photos_Center')
 CREATE INDEX IX_Photos_Center ON dbo.Photos(DiveCenterId,IsCover,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Photos') AND name='IX_Photos_Activity')
 CREATE INDEX IX_Photos_Activity ON dbo.Photos(ActivityId,IsCover,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Photos') AND name='UX_Photos_CenterCover')
 CREATE UNIQUE INDEX UX_Photos_CenterCover ON dbo.Photos(DiveCenterId)
  WHERE DiveCenterId IS NOT NULL AND IsCover=1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Photos') AND name='UX_Photos_ActivityCover')
 CREATE UNIQUE INDEX UX_Photos_ActivityCover ON dbo.Photos(ActivityId)
  WHERE ActivityId IS NOT NULL AND IsCover=1;
GO


-- 011_CreateApplicationsAndNotifications.sql
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
GO


-- 012_AddLocationsAndReservationContacts.sql
USE DiveMarketplaceDb;
SET QUOTED_IDENTIFIER ON;
IF COL_LENGTH('dbo.DiveCenters','MapLocation') IS NULL
 ALTER TABLE dbo.DiveCenters ADD MapLocation nvarchar(500) NULL;
IF COL_LENGTH('dbo.Activities','MapLocation') IS NULL
 ALTER TABLE dbo.Activities ADD MapLocation nvarchar(500) NULL;
IF COL_LENGTH('dbo.Reservations','ContactEmail') IS NULL
 ALTER TABLE dbo.Reservations ADD ContactEmail nvarchar(200) NULL;
IF COL_LENGTH('dbo.Reservations','ContactPhone') IS NULL
 ALTER TABLE dbo.Reservations ADD ContactPhone nvarchar(50) NULL;
IF COL_LENGTH('dbo.Reservations','ContactWhatsApp') IS NULL
 ALTER TABLE dbo.Reservations ADD ContactWhatsApp nvarchar(50) NULL;
IF COL_LENGTH('dbo.Reservations','ContactByEmail') IS NULL
 ALTER TABLE dbo.Reservations ADD ContactByEmail bit NOT NULL CONSTRAINT DF_Reservations_ContactEmail DEFAULT 0;
IF COL_LENGTH('dbo.Reservations','ContactByPhone') IS NULL
 ALTER TABLE dbo.Reservations ADD ContactByPhone bit NOT NULL CONSTRAINT DF_Reservations_ContactPhone DEFAULT 0;
IF COL_LENGTH('dbo.Reservations','ContactByWhatsApp') IS NULL
 ALTER TABLE dbo.Reservations ADD ContactByWhatsApp bit NOT NULL CONSTRAINT DF_Reservations_ContactWhatsApp DEFAULT 0;
GO


-- 013_AddApplicationMapLocation.sql
-- Owner application location is copied to DiveCenters on approval.
IF COL_LENGTH('dbo.DiveCenterApplications', 'MapLocation') IS NULL
    ALTER TABLE dbo.DiveCenterApplications ADD MapLocation NVARCHAR(500) NULL;
GO


-- 014_StorePhotoContent.sql
-- Run against the application's database. Existing photo URLs remain valid until imported.
SET XACT_ABORT ON;
IF OBJECT_ID('dbo.Photos','U') IS NULL
    THROW 50001, 'The dbo.Photos table must exist.', 1;
IF COL_LENGTH('dbo.Photos','ImageData') IS NULL
    ALTER TABLE dbo.Photos ADD ImageData varbinary(max) NULL;
IF COL_LENGTH('dbo.Photos','ContentType') IS NULL
    ALTER TABLE dbo.Photos ADD ContentType nvarchar(100) NULL;
GO

