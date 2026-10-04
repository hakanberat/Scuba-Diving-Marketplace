using Dapper;
using DiveMarketplace.Data;
using DiveMarketplace.Models;
using DiveMarketplace.ViewModels;
namespace DiveMarketplace.Services;

public class ActivityService(SqlConnectionFactory factory)
{
    private const string Select = "SELECT a.*, (SELECT TOP(1) p.ImageUrl FROM Photos p WHERE p.ActivityId=a.Id ORDER BY p.IsCover DESC,p.Id) CoverPhotoUrl, c.Name CategoryName,d.Name DiveCenterName FROM Activities a JOIN ActivityCategories c ON c.Id=a.ActivityCategoryId JOIN DiveCenters d ON d.Id=a.DiveCenterId ";
    public async Task<List<DiveActivity>> SearchAsync(ActivitySearchViewModel filter, int? centerId = null)
    {
        using var db = factory.CreateConnection();
        return (await db.QueryAsync<DiveActivity>(Select + """
   WHERE a.IsActive=1 AND d.IsActive=1 AND c.IsActive=1
   AND (@centerId IS NULL OR a.DiveCenterId=@centerId)
   AND (@search IS NULL OR a.Name LIKE @search OR a.ShortDescription LIKE @search OR d.Name LIKE @search)
   AND (@country IS NULL OR a.Country=@country) AND (@city IS NULL OR a.City=@city)
   AND (@categoryId IS NULL OR a.ActivityCategoryId=@categoryId)
   AND (@minPrice IS NULL OR a.Price>=@minPrice) AND (@maxPrice IS NULL OR a.Price<=@maxPrice)
   ORDER BY a.CreatedAt DESC,a.Id DESC
   """, new { centerId, search = string.IsNullOrWhiteSpace(filter.Search) ? null : "%" + filter.Search.Trim() + "%", country = string.IsNullOrWhiteSpace(filter.Country) ? null : filter.Country.Trim(), city = string.IsNullOrWhiteSpace(filter.City) ? null : filter.City.Trim(), filter.CategoryId, filter.MinPrice, filter.MaxPrice })).ToList();
    }
    public async Task<DiveActivity?> GetPublicAsync(int id)
    {
        using var db = factory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<DiveActivity>(Select + "WHERE a.Id=@id AND a.IsActive=1 AND d.IsActive=1 AND c.IsActive=1", new { id });
    }
    public async Task<List<DiveActivity>> GetManagedAsync(int userId, bool admin)
    {
        using var db = factory.CreateConnection();
        return (await db.QueryAsync<DiveActivity>(Select + $"WHERE {CenterAccessService.Scope} ORDER BY a.Name", new { userId, admin })).ToList();
    }
    public async Task<ActivityEditViewModel?> GetEditAsync(int id, int userId, bool admin)
    {
        using var db = factory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<ActivityEditViewModel>($"SELECT a.* FROM Activities a JOIN DiveCenters d ON d.Id=a.DiveCenterId WHERE a.Id=@id AND {CenterAccessService.Scope}", new { id, userId, admin });
    }
    public async Task<bool> SaveAsync(ActivityEditViewModel m, int userId, bool admin)
    {
        using var db = factory.CreateConnection();
        var scope = CenterAccessService.Scope;
        var sql = m.Id == 0 ? $"""
   INSERT Activities(DiveCenterId,ActivityCategoryId,Name,ShortDescription,Description,Country,City,Location,MapLocation,Price,CurrencyCode,Duration,MinimumAge,MinimumCertification,MaxParticipants,Included,NotIncluded,Requirements,MainImageUrl,IsActive)
   SELECT @DiveCenterId,@ActivityCategoryId,@Name,@ShortDescription,@Description,@Country,@City,@Location,@MapLocation,@Price,@CurrencyCode,@Duration,@MinimumAge,@MinimumCertification,@MaxParticipants,@Included,@NotIncluded,@Requirements,@MainImageUrl,@IsActive
   FROM DiveCenters d WHERE d.Id=@DiveCenterId AND {scope} AND EXISTS(SELECT 1 FROM ActivityCategories WHERE Id=@ActivityCategoryId AND IsActive=1)
   """ : $"""
   UPDATE a SET ActivityCategoryId=@ActivityCategoryId,Name=@Name,ShortDescription=@ShortDescription,Description=@Description,Country=@Country,City=@City,Location=@Location,MapLocation=@MapLocation,Price=@Price,CurrencyCode=@CurrencyCode,Duration=@Duration,MinimumAge=@MinimumAge,MinimumCertification=@MinimumCertification,MaxParticipants=@MaxParticipants,Included=@Included,NotIncluded=@NotIncluded,Requirements=@Requirements,MainImageUrl=@MainImageUrl,IsActive=@IsActive,UpdatedAt=SYSUTCDATETIME()
   FROM Activities a JOIN DiveCenters d ON d.Id=a.DiveCenterId
   WHERE a.Id=@Id AND a.DiveCenterId=@DiveCenterId AND {scope}
   AND EXISTS(SELECT 1 FROM ActivityCategories WHERE Id=@ActivityCategoryId AND (IsActive=1 OR Id=a.ActivityCategoryId))
   """;
        var parameters = new DynamicParameters(m); parameters.Add("userId", userId); parameters.Add("admin", admin);
        return await db.ExecuteAsync(sql, parameters) == 1;
    }
    public async Task<bool> SetActiveAsync(int id, bool active, int userId, bool admin)
    {
        using var db = factory.CreateConnection();
        return await db.ExecuteAsync($"UPDATE a SET IsActive=@active,UpdatedAt=SYSUTCDATETIME() FROM Activities a JOIN DiveCenters d ON d.Id=a.DiveCenterId WHERE a.Id=@id AND {CenterAccessService.Scope}", new { id, active, userId, admin }) == 1;
    }
}
