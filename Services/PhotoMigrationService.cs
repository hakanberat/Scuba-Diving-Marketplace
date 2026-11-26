using System.Text.RegularExpressions;
using Dapper;
using DiveMarketplace.Data;
using DiveMarketplace.Models;

namespace DiveMarketplace.Services;

public class PhotoMigrationService(SqlConnectionFactory factory, IWebHostEnvironment environment, PhotoUploadService uploads)
{
    public async Task<int> MigrateAsync()
    {
        using var db = factory.CreateConnection();
        await db.OpenAsync();
        await db.ExecuteAsync("""
            SET XACT_ABORT ON;

            IF OBJECT_ID('dbo.Photos','U') IS NULL
                THROW 50001, 'The dbo.Photos table must exist.', 1;

            IF COL_LENGTH('dbo.Photos','ImageData') IS NULL
                ALTER TABLE dbo.Photos
                ADD ImageData varbinary(max) NULL;

            IF COL_LENGTH('dbo.Photos','ContentType') IS NULL
                ALTER TABLE dbo.Photos
                ADD ContentType nvarchar(100) NULL;
            """);

        await using var transaction = await db.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var photos = (await db.QueryAsync<Photo>("""
            SELECT Id,ImageUrl FROM Photos WITH (UPDLOCK,HOLDLOCK)
            WHERE ImageData IS NULL AND ImageUrl LIKE '/uploads/photos/%'
            """, transaction: transaction)).ToList();
        foreach (var photo in photos)
        {
            if (!Regex.IsMatch(photo.ImageUrl, "^/uploads/photos/[a-f0-9]{32}\\.(jpg|png|webp)$"))
                throw new InvalidOperationException($"Photo {photo.Id} has an unexpected local path; no photos were imported.");
            var path = Path.Combine(environment.WebRootPath, "uploads", "photos", Path.GetFileName(photo.ImageUrl));
            await using var file = File.OpenRead(path);
            var content = await uploads.ReadAsync(file, file.Length);
            var url = $"/Photos/Image/{photo.Id}";
            var changed = await db.ExecuteAsync("""
                UPDATE Photos SET ImageData=@ImageData,ContentType=@ContentType,ImageUrl=@url
                WHERE Id=@Id AND ImageUrl=@oldUrl AND ImageData IS NULL
                """, new { photo.Id, content.ImageData, content.ContentType, url, oldUrl = photo.ImageUrl }, transaction);
            if (changed != 1)
                throw new InvalidOperationException("A photo changed during migration; import was rolled back.");
            var saved = await db.QuerySingleAsync<byte[]>("SELECT ImageData FROM Photos WHERE Id=@Id", new { photo.Id }, transaction);
            if (!saved.AsSpan().SequenceEqual(content.ImageData))
                throw new InvalidOperationException("Photo data verification failed; import was rolled back.");
            // Preserve manually reused links to an uploaded gallery image as well.
            await db.ExecuteAsync("""
                UPDATE Activities SET MainImageUrl=@url WHERE MainImageUrl=@oldUrl;
                UPDATE DiveCenters SET LogoUrl=@url WHERE LogoUrl=@oldUrl;
                UPDATE Instructors SET PhotoUrl=@url WHERE PhotoUrl=@oldUrl;
                """, new { url, oldUrl = photo.ImageUrl }, transaction);
        }
        await transaction.CommitAsync();
        return photos.Count;
    }
}

