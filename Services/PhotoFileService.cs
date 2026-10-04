using System.Text;
using System.Text.RegularExpressions;

namespace DiveMarketplace.Services;

public class PhotoFileService(IWebHostEnvironment environment)
{
    public const int MaximumBytes = 5 * 1024 * 1024;
    public async Task<string> SaveAsync(IFormFile file)
    {
        if (file.Length < 12 || file.Length > MaximumBytes)
            throw new ArgumentException("Choose an image up to 5 MB (JPEG, PNG or WebP).");
        await using var input = file.OpenReadStream();
        var header = new byte[12];
        await input.ReadExactlyAsync(header);
        var extension = header.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}) ? ".png"
            : header[0] == 255 && header[1] == 216 && header[2] == 255 ? ".jpg"
            : Encoding.ASCII.GetString(header, 0, 4) == "RIFF" && Encoding.ASCII.GetString(header, 8, 4) == "WEBP" ? ".webp" : null;
        if (extension is null) throw new ArgumentException("Only JPEG, PNG and WebP image files are supported.");
        var folder = Path.Combine(environment.WebRootPath, "uploads", "photos");
        Directory.CreateDirectory(folder);
        var name = Guid.NewGuid().ToString("N") + extension;
        var path = Path.Combine(folder, name);
        try
        {
            await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await output.WriteAsync(header);
            var buffer = new byte[81920];
            var total = header.Length;
            int read;
            while ((read = await input.ReadAsync(buffer)) > 0)
            {
                total += read;
                if (total > MaximumBytes) throw new ArgumentException("The image exceeds the 5 MB limit.");
                await output.WriteAsync(buffer.AsMemory(0, read));
            }
        }
        catch { File.Delete(path); throw; }
        return "/uploads/photos/" + name;
    }

    public void Delete(string url)
    {
        if (!Regex.IsMatch(url, "^/uploads/photos/[a-f0-9]{32}\\.(jpg|png|webp)$")) return;
        File.Delete(Path.Combine(environment.WebRootPath, "uploads", "photos", Path.GetFileName(url)));
    }
}
