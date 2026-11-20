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
            Photos = (await db.QueryAsync<Photo>($"SELECT Id,ImageUrl,Caption,IsCover FROM Photos WHERE {Parent(target)}=@id ORDER BY IsCover DESC,Id", new { id })).ToList()
        };
    }

    public async Task<List<Photo>> GetPublicAsync(PhotoTarget target, int id)
    {
        using var db = factory.CreateConnection();
        var sql = target == PhotoTarget.Center
            ? "SELECT p.Id,p.ImageUrl,p.Caption,p.IsCover FROM Photos p JOIN DiveCenters d ON d.Id=p.DiveCenterId WHERE d.Id=@id AND d.IsActive=1"
            : "SELECT p.Id,p.ImageUrl,p.Caption,p.IsCover FROM Photos p JOIN Activities a ON a.Id=p.ActivityId JOIN DiveCenters d ON d.Id=a.DiveCenterId JOIN ActivityCategories c ON c.Id=a.ActivityCategoryId WHERE a.Id=@id AND a.IsActive=1 AND d.IsActive=1 AND c.IsActive=1";
        return (await db.QueryAsync<Photo>(sql + " ORDER BY p.IsCover DESC,p.Id", new { id })).ToList();
    }

    public async Task<bool> AddManyAsync(PhotoInputViewModel input, IReadOnlyList<PhotoContent> uploads, int userId, bool admin)
    {
        var photoCount = uploads.Count > 0 ? uploads.Count : 1;
        if(photoCount>MaximumPhotos || (uploads.Count==0 && string.IsNullOrWhiteSpace(input.ImageUrl))) return false;
        using var db = factory.CreateConnection();
        await db.OpenAsync();
        await using var tx = await db.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        // Lock the parent while checking ownership/count so parallel uploads respect the limit.
        var resourceSql = ResourceSql(input.Target).Replace("DiveCenters d", "DiveCenters d WITH (UPDLOCK)");
        var name = await db.QuerySingleOrDefaultAsync<string>(resourceSql, new { id = input.ResourceId, userId, admin }, tx);
        if (name is null) return false;
        var count = await db.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Photos WHERE {Parent(input.Target)}=@id", new { id = input.ResourceId }, tx);
        if (count+photoCount > MaximumPhotos) return false;
        for(var index=0;index<photoCount;index++)
        {
            var upload = uploads.Count > 0 ? uploads[index] : null;
            var photoId = await db.QuerySingleAsync<int>($"""
                INSERT Photos({Parent(input.Target)},ImageUrl,Caption,IsCover,ImageData,ContentType)
                OUTPUT INSERTED.Id VALUES(@id,@url,@caption,@cover,@data,@contentType)
                """, new { id = input.ResourceId, url = upload is null ? input.ImageUrl!.Trim() : "",
                    caption = input.Caption, cover = count == 0 && index==0,
                    data = upload?.ImageData, contentType = upload?.ContentType }, tx);
            if (upload is not null)
                await db.ExecuteAsync("UPDATE Photos SET ImageUrl=@url WHERE Id=@photoId",
                    new { url = $"/Photos/Image/{photoId}", photoId }, tx);
        }
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

    public async Task<PhotoContent?> GetImageAsync(int id, int userId, bool admin, bool manager)
    {
        using var db = factory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<PhotoContent>($"""
            SELECT p.ImageData,p.ContentType FROM Photos p
            LEFT JOIN Activities a ON a.Id=p.ActivityId
            LEFT JOIN ActivityCategories c ON c.Id=a.ActivityCategoryId
            JOIN DiveCenters d ON d.Id=COALESCE(p.DiveCenterId,a.DiveCenterId)
            WHERE p.Id=@id AND p.ImageData IS NOT NULL AND p.ContentType IS NOT NULL
            AND ((d.IsActive=1 AND (p.ActivityId IS NULL OR (a.IsActive=1 AND c.IsActive=1)))
                OR (@manager=1 AND {CenterAccessService.Scope}))
            """, new { id, userId, admin, manager });
    }

    public async Task<bool> DeleteAsync(PhotoTarget target, int id, int photoId, int userId, bool admin)
    {
        using var db = factory.CreateConnection();
        var scope = target == PhotoTarget.Center
            ? "EXISTS(SELECT 1 FROM DiveCenters d WHERE d.Id=p.DiveCenterId AND " + CenterAccessService.Scope + ")"
            : "EXISTS(SELECT 1 FROM Activities a JOIN DiveCenters d ON d.Id=a.DiveCenterId WHERE a.Id=p.ActivityId AND " + CenterAccessService.Scope + ")";
        return await db.ExecuteAsync($"DELETE p FROM Photos p WHERE p.Id=@photoId AND p.{Parent(target)}=@id AND {scope}", new { id, photoId, userId, admin }) == 1;
    }
}
