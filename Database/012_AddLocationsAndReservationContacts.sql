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
