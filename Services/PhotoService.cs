using Dapper;
using DiveMarketplace.Data;
using DiveMarketplace.Models;
using DiveMarketplace.ViewModels;

namespace DiveMarketplace.Services;

public class PhotoService(SqlConnectionFactory factory)
{
    public const int MaximumPhotos = 20;
    // SQL fragments are selected only from the enum, never from request text.
    private static string Parent(PhotoTarget target) => target == PhotoTarget.Center ? "DiveCenterId" : "ActivityId";
    private static string ResourceSql(PhotoTarget target) => target == PhotoTarget.Center
        ? "SELECT d.Name FROM DiveCenters d WHERE d.Id=@id AND " + CenterAccessService.Scope
        : "SELECT a.Name FROM Activities a JOIN DiveCenters d ON d.Id=a.DiveCenterId WHERE a.Id=@id AND " + CenterAccessService.Scope;

    public async Task<PhotoManagementViewModel?> GetManagedAsync(PhotoTarget target, int id, int userId, bool admin)
    {
        using var db = factory.CreateConnection();
        var name = await db.QuerySingleOrDefaultAsync<string>(ResourceSql(target), new { id, userId, admin });
        if (name is null) return null;
        return new PhotoManagementViewModel
        {
            Name = name, Input = new() { Target = target, ResourceId = id },
            Photos = (await db.QueryAsync<Photo>($"SELECT * FROM Photos WHERE {Parent(target)}=@id ORDER BY IsCover DESC,Id", new { id })).ToList()
        };
    }

    public async Task<List<Photo>> GetPublicAsync(PhotoTarget target, int id)
    {
        using var db = factory.CreateConnection();
        var sql = target == PhotoTarget.Center
            ? "SELECT p.* FROM Photos p JOIN DiveCenters d ON d.Id=p.DiveCenterId WHERE d.Id=@id AND d.IsActive=1"
            : "SELECT p.* FROM Photos p JOIN Activities a ON a.Id=p.ActivityId JOIN DiveCenters d ON d.Id=a.DiveCenterId JOIN ActivityCategories c ON c.Id=a.ActivityCategoryId WHERE a.Id=@id AND a.IsActive=1 AND d.IsActive=1 AND c.IsActive=1";
        return (await db.QueryAsync<Photo>(sql + " ORDER BY p.IsCover DESC,p.Id", new { id })).ToList();
    }

    public async Task<bool> AddAsync(PhotoInputViewModel input, string url, int userId, bool admin)
        => await AddManyAsync(input,[url],userId,admin);

    public async Task<bool> AddManyAsync(PhotoInputViewModel input, IReadOnlyList<string> urls, int userId, bool admin)
    {
        if(urls.Count==0 || urls.Count>MaximumPhotos) return false;
        using var db = factory.CreateConnection();
        await db.OpenAsync();
        await using var tx = await db.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        // Lock the parent while checking ownership/count so parallel uploads respect the limit.
        var resourceSql = ResourceSql(input.Target).Replace("DiveCenters d", "DiveCenters d WITH (UPDLOCK)");
        var name = await db.QuerySingleOrDefaultAsync<string>(resourceSql, new { id = input.ResourceId, userId, admin }, tx);
        if (name is null) return false;
        var count = await db.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Photos WHERE {Parent(input.Target)}=@id", new { id = input.ResourceId }, tx);
        if (count+urls.Count > MaximumPhotos) return false;
        for(var index=0;index<urls.Count;index++)
            await db.ExecuteAsync($"INSERT Photos({Parent(input.Target)},ImageUrl,Caption,IsCover) VALUES(@id,@url,@caption,@cover)",
                new { id = input.ResourceId, url=urls[index], caption = input.Caption, cover = count == 0 && index==0 }, tx);
        await tx.CommitAsync();
        return true;
    }

    public async Task<bool> SetCoverAsync(PhotoTarget target, int id, int photoId, int userId, bool admin)
    {
        using var db = factory.CreateConnection();
        await db.OpenAsync();
        await using var tx = await db.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var resourceSql = ResourceSql(target).Replace("DiveCenters d", "DiveCenters d WITH (UPDLOCK)");
        if (await db.QuerySingleOrDefaultAsync<string>(resourceSql, new { id, userId, admin }, tx) is null) return false;
        if (await db.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Photos WHERE Id=@photoId AND {Parent(target)}=@id", new { photoId, id }, tx) != 1) return false;
        await db.ExecuteAsync($"UPDATE Photos SET IsCover=0 WHERE {Parent(target)}=@id; UPDATE Photos SET IsCover=1 WHERE Id=@photoId AND {Parent(target)}=@id", new { id, photoId }, tx);
        await tx.CommitAsync();
        return true;
    }

    public async Task<string?> DeleteAsync(PhotoTarget target, int id, int photoId, int userId, bool admin)
    {
        using var db = factory.CreateConnection();
        var scope = target == PhotoTarget.Center
            ? "EXISTS(SELECT 1 FROM DiveCenters d WHERE d.Id=p.DiveCenterId AND " + CenterAccessService.Scope + ")"
            : "EXISTS(SELECT 1 FROM Activities a JOIN DiveCenters d ON d.Id=a.DiveCenterId WHERE a.Id=p.ActivityId AND " + CenterAccessService.Scope + ")";
        return await db.QuerySingleOrDefaultAsync<string>($"DELETE p OUTPUT DELETED.ImageUrl FROM Photos p WHERE p.Id=@photoId AND p.{Parent(target)}=@id AND {scope}", new { id, photoId, userId, admin });
    }
}
