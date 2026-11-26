using System.ComponentModel.DataAnnotations;
namespace DiveMarketplace.ViewModels;
public class ProfileViewModel
{
    [Required,StringLength(100)] public string FirstName { get; set; } = "";
    [Required,StringLength(100)] public string LastName { get; set; } = "";
    [Required,EmailAddress,StringLength(200)] public string Email { get; set; } = "";
    [Phone,StringLength(50)] public string? Phone { get; set; }
}
