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