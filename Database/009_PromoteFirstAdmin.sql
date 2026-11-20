USE DiveMarketplaceDb;
-- Register normally first, then replace the email below and execute manually in SSMS.
DECLARE @Email nvarchar(200)=N'REPLACE-WITH-YOUR-REGISTERED-EMAIL';
IF @Email=N'REPLACE-WITH-YOUR-REGISTERED-EMAIL'
 THROW 50001,'Set @Email to your registered email before running this script.',1;
IF NOT EXISTS(SELECT 1 FROM AppUsers WHERE Email=@Email AND IsActive=1)
 THROW 50002,'No active registered user was found with that email.',1;
UPDATE AppUsers SET Role='Admin' WHERE Email=@Email AND IsActive=1;
-- Log in again afterward. Role changes invalidate existing authentication cookies.
