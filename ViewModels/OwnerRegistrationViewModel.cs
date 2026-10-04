using System.ComponentModel.DataAnnotations;
namespace DiveMarketplace.ViewModels;
public class OwnerRegistrationViewModel : RegisterViewModel
{
    [Required, StringLength(200)] public string CenterName { get; set; } = "";
    [StringLength(20000)] public string? Description { get; set; }
    [Required, StringLength(100)] public string Country { get; set; } = "";
    [Required, StringLength(100)] public string City { get; set; } = "";
    [StringLength(500)] public string? Address { get; set; }
    [Required, StringLength(500)] public string MapLocation { get; set; } = "";
    [WebAddress, StringLength(300)] public string? Website { get; set; }
}
public class CenterApplicationViewModel
{
    public int Id { get; set; }
    public int AppUserId { get; set; }
    public string Name { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Description { get; set; }
    public string Country { get; set; } = "";
    public string City { get; set; } = "";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string? MapLocation { get; set; }
    public string Status { get; set; } = "";
    public string? ReviewNotes { get; set; }
    public int? DiveCenterId { get; set; }
    public DateTime CreatedAt { get; set; }
}
