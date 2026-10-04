using Dapper;
using DiveMarketplace.Data;
using DiveMarketplace.Models;
using DiveMarketplace.ViewModels;
namespace DiveMarketplace.Services;

public class CenterAccessService(SqlConnectionFactory factory)
{
    public const string Scope = "(@admin=1 OR EXISTS(SELECT 1 FROM DiveCenterUsers access WHERE access.DiveCenterId=d.Id AND access.AppUserId=@userId))";
    public async Task<bool> CanManageAsync(int centerId, int userId, bool admin)
    {
        using var db = factory.CreateConnection();
        return await db.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM DiveCenters d WHERE d.Id=@centerId AND {Scope}", new { centerId, userId, admin }) > 0;
    }
    public async Task<List<DiveCenter>> GetCentersAsync(int userId, bool admin)
    {
        using var db = factory.CreateConnection();
        return (await db.QueryAsync<DiveCenter>($"SELECT d.* FROM DiveCenters d WHERE {Scope} ORDER BY d.Name", new { userId, admin })).ToList();
    }
    public async Task<CenterEditViewModel?> GetProfileAsync(int id, int userId, bool admin)
    {
        using var db = factory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<CenterEditViewModel>($"SELECT d.* FROM DiveCenters d WHERE d.Id=@id AND {Scope}", new { id, userId, admin });
    }
    public async Task<bool> SaveProfileAsync(CenterEditViewModel model, int userId, bool admin)
    {
        using var db = factory.CreateConnection();
        return await db.ExecuteAsync($"""
   UPDATE d SET Name=@Name,Description=@Description,Country=@Country,City=@City,Address=@Address,
   Phone=@Phone,Email=@Email,Website=@Website,LogoUrl=@LogoUrl,MapLocation=@MapLocation
   FROM DiveCenters d WHERE d.Id=@Id AND {Scope}
   """, new { model.Id, model.Name, model.Description, model.Country, model.City, model.Address, model.Phone, model.Email, Website=WebAddressAttribute.Normalize(model.Website), model.LogoUrl, model.MapLocation,userId, admin }) == 1;
    }
    public async Task<int> CreateAsync(CenterEditViewModel model, int userId)
    {
        using var db = factory.CreateConnection();
        model.Website=WebAddressAttribute.Normalize(model.Website);
        await db.OpenAsync();
        await using var transaction = await db.BeginTransactionAsync();
        var id = await db.QuerySingleAsync<int>("""
   INSERT DiveCenters(Name,Description,Country,City,Address,Phone,Email,Website,LogoUrl,MapLocation,IsActive,CreatedAt)
   OUTPUT INSERTED.Id VALUES(@Name,@Description,@Country,@City,@Address,@Phone,@Email,@Website,@LogoUrl,@MapLocation,1,SYSUTCDATETIME())
   """, model, transaction);
        await db.ExecuteAsync("INSERT DiveCenterUsers(DiveCenterId,AppUserId,IsOwner) VALUES(@id,@userId,1);", new { id, userId }, transaction);
        await transaction.CommitAsync();
        return id;
    }
    public async Task<bool> AssignAsync(CenterUserViewModel model)
    {
        using var db = factory.CreateConnection();
        return await db.ExecuteAsync("""
   IF EXISTS(SELECT 1 FROM AppUsers WHERE Id=@AppUserId AND Role='DiveCenter' AND IsActive=1)
    AND EXISTS(SELECT 1 FROM DiveCenters WHERE Id=@DiveCenterId)
   BEGIN
    IF EXISTS(SELECT 1 FROM DiveCenterUsers WHERE DiveCenterId=@DiveCenterId AND AppUserId=@AppUserId)
     UPDATE DiveCenterUsers SET IsOwner=@IsOwner WHERE DiveCenterId=@DiveCenterId AND AppUserId=@AppUserId;
    ELSE INSERT DiveCenterUsers(DiveCenterId,AppUserId,IsOwner) VALUES(@DiveCenterId,@AppUserId,@IsOwner);
   END
   """, model) > 0;
    }
}
