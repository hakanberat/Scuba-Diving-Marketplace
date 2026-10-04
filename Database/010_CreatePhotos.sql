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
