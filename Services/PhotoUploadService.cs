using System.Text;
using DiveMarketplace.Models;

namespace DiveMarketplace.Services;

public class PhotoUploadService
{
    public const int MaximumBytes = 5 * 1024 * 1024;

    public async Task<PhotoContent> ReadAsync(IFormFile file)
    {
        await using var input = file.OpenReadStream();
        return await ReadAsync(input, file.Length);
    }

    public async Task<PhotoContent> ReadAsync(Stream input, long length)
    {
        if (length < 12 || length > MaximumBytes)
            throw new ArgumentException("Choose an image up to 5 MB (JPEG, PNG or WebP).");
        var header = new byte[12];
        await input.ReadExactlyAsync(header);
        var contentType = header.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}) ? "image/png"
            : header[0] == 255 && header[1] == 216 && header[2] == 255 ? "image/jpeg"
            : Encoding.ASCII.GetString(header, 0, 4) == "RIFF" && Encoding.ASCII.GetString(header, 8, 4) == "WEBP" ? "image/webp" : null;
        if (contentType is null)
            throw new ArgumentException("Only JPEG, PNG and WebP image files are supported.");
        using var output = new MemoryStream();
        await output.WriteAsync(header);
        var buffer = new byte[81920];
        int read;
        while ((read = await input.ReadAsync(buffer)) > 0)
        {
            if (output.Length + read > MaximumBytes)
                throw new ArgumentException("The image exceeds the 5 MB limit.");
            await output.WriteAsync(buffer.AsMemory(0, read));
        }
        return new PhotoContent { ImageData = output.ToArray(), ContentType = contentType };
    }
}
