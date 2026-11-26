using System.ComponentModel.DataAnnotations;
using DiveMarketplace.Models;

namespace DiveMarketplace.ViewModels;

public class PhotoInputViewModel : IValidatableObject
{
    [EnumDataType(typeof(PhotoTarget))] public PhotoTarget Target { get; set; }
    [Range(1, int.MaxValue)] public int ResourceId { get; set; }
    public IFormFile? File { get; set; }
    public List<IFormFile> Files { get; set; } = [];
    public IEnumerable<IFormFile> GetUploads() => File is null ? Files : Files.Prepend(File);
    [StringLength(500)] public string? ImageUrl { get; set; }
    [StringLength(200)] public string? Caption { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!GetUploads().Any() == string.IsNullOrWhiteSpace(ImageUrl))
            yield return new ValidationResult("Choose a photo file or enter an image URL, but not both.");
        if(GetUploads().Count()>20)
            yield return new ValidationResult("Select no more than 20 photos at a time.");
        if (!string.IsNullOrWhiteSpace(ImageUrl) &&
            (!Uri.TryCreate(ImageUrl, UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)))
            yield return new ValidationResult("Use an http or https image URL.", [nameof(ImageUrl)]);
    }
}

public class PhotoManagementViewModel
{
    public PhotoInputViewModel Input { get; set; } = new();
    public string Name { get; set; } = "";
    public List<Photo> Photos { get; set; } = [];
}

public class ActivityDetailsViewModel
{
    public DiveActivity Activity { get; set; } = new();
    public List<Photo> Photos { get; set; } = [];
}
