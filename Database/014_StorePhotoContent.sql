-- Run against the application's database. Existing photo URLs remain valid until imported.
SET XACT_ABORT ON;
IF OBJECT_ID('dbo.Photos','U') IS NULL
    THROW 50001, 'Run 010_CreatePhotos.sql before this migration.', 1;
IF COL_LENGTH('dbo.Photos','ImageData') IS NULL
    ALTER TABLE dbo.Photos ADD ImageData varbinary(max) NULL;
IF COL_LENGTH('dbo.Photos','ContentType') IS NULL
    ALTER TABLE dbo.Photos ADD ContentType nvarchar(100) NULL;
