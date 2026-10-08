namespace DiveMarketplace.Models;

public class DiveCenter
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Country { get; set; }

    public string? City { get; set; }

    public string? Address { get; set; }
    public string? MapLocation { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Website { get; set; }

    public string? LogoUrl { get; set; }
    public string? CoverPhotoUrl { get; set; }
    public bool UsesRepresentativeImage => string.IsNullOrWhiteSpace(CoverPhotoUrl);
    public string? DisplayImageUrl => UsesRepresentativeImage ? MarketplaceImages.ForCenter(Id) : CoverPhotoUrl!;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
}
