USE DiveMarketplaceDb;
-- Optional demonstration listings for the existing Dive Hub profile. Review/edit prices before publishing.
DECLARE @CenterId int=(SELECT TOP(1) Id FROM dbo.DiveCenters WHERE Name=N'Dive Hub' ORDER BY Id);
IF @CenterId IS NOT NULL
BEGIN
 IF NOT EXISTS(SELECT 1 FROM Activities WHERE DiveCenterId=@CenterId AND Name=N'Discover Scuba Diving')
 INSERT Activities(DiveCenterId,ActivityCategoryId,Name,ShortDescription,Description,Country,City,Location,Price,CurrencyCode,Duration,MinimumAge,MaxParticipants,Included,Requirements)
 SELECT @CenterId,Id,N'Discover Scuba Diving',N'A first underwater adventure with a dive professional.',N'Demonstration listing: an introductory scuba experience. Contact the center to confirm the actual itinerary and price.',N'Cyprus',N'Famagusta',N'Dive Hub',85,'EUR',N'3 hours',10,4,N'Equipment and instructor supervision',N'Medical questionnaire and swimming comfort required.' FROM ActivityCategories WHERE Slug='try-dive';
 IF NOT EXISTS(SELECT 1 FROM Activities WHERE DiveCenterId=@CenterId AND Name=N'PADI Open Water Diver')
 INSERT Activities(DiveCenterId,ActivityCategoryId,Name,ShortDescription,Description,Country,City,Location,Price,CurrencyCode,Duration,MinimumAge,MaxParticipants,Included,Requirements)
 SELECT @CenterId,Id,N'PADI Open Water Diver',N'Build your skills and discover recreational diving.',N'Demonstration listing: entry-level scuba course. Confirm course dates, certification fees and actual price with the center.',N'Cyprus',N'Famagusta',N'Dive Hub',420,'EUR',N'4 days',10,4,N'Instructor-led training and equipment',N'Medical questionnaire and swimming assessment.' FROM ActivityCategories WHERE Slug='scuba-course';
END;
