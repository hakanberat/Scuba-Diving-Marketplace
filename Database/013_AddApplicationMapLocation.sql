-- Owner application location is copied to DiveCenters on approval.
IF COL_LENGTH('dbo.DiveCenterApplications', 'MapLocation') IS NULL
    ALTER TABLE dbo.DiveCenterApplications ADD MapLocation NVARCHAR(500) NULL;
