using System.ComponentModel.DataAnnotations;
using DiveMarketplace.Models;
namespace DiveMarketplace.ViewModels;
public class CenterEditViewModel
{
 public int Id { get; set; }
 [Required,StringLength(200)] public string Name { get; set; } = "";
 [StringLength(20000)] public string? Description { get; set; }
 [StringLength(100)] public string? Country { get; set; }
 [StringLength(100)] public string? City { get; set; }
 [StringLength(500)] public string? Address { get; set; }
 [StringLength(500)] public string? MapLocation { get; set; }
 [StringLength(50)] public string? Phone { get; set; }
 [EmailAddress,StringLength(200)] public string? Email { get; set; }
 [WebAddress,StringLength(300)] public string? Website { get; set; }
 [Url,StringLength(500)] public string? LogoUrl { get; set; }
}
public class InstructorEditViewModel
{
 public int Id { get; set; }
 [Range(1,int.MaxValue)] public int DiveCenterId { get; set; }
 [Required,StringLength(100)] public string FirstName { get; set; } = "";
 [Required,StringLength(100)] public string LastName { get; set; } = "";
 [StringLength(20000)] public string? Bio { get; set; }
 [Url,StringLength(500)] public string? PhotoUrl { get; set; }
 [StringLength(50)] public string? CertificationAgency { get; set; }
 [StringLength(100)] public string? InstructorNumber { get; set; }
 public bool IsActive { get; set; } = true;
 public List<DiveCenter> Centers { get; set; } = [];
}
public class ActivityEditViewModel
{
 public int Id { get; set; }
 [Range(1,int.MaxValue)] public int DiveCenterId { get; set; }
 [Range(1,int.MaxValue)] public int ActivityCategoryId { get; set; }
 [Required,StringLength(200)] public string Name { get; set; } = "";
 [StringLength(500)] public string? ShortDescription { get; set; }
 [StringLength(20000)] public string? Description { get; set; }
 [StringLength(100)] public string? Country { get; set; }
 [StringLength(100)] public string? City { get; set; }
 [StringLength(500)] public string? Location { get; set; }
 [StringLength(500)] public string? MapLocation { get; set; }
 [Range(typeof(decimal),"0","100000000")] public decimal Price { get; set; }
 [Required,RegularExpression("^(TRY|USD|EUR|GBP)$")] public string CurrencyCode { get; set; } = "EUR";
 [StringLength(100)] public string? Duration { get; set; }
 [Range(0,120)] public int? MinimumAge { get; set; }
 [StringLength(200)] public string? MinimumCertification { get; set; }
 [Range(1,10000)] public int? MaxParticipants { get; set; }
 [StringLength(20000)] public string? Included { get; set; }
 [StringLength(20000)] public string? NotIncluded { get; set; }
 [StringLength(20000)] public string? Requirements { get; set; }
 [Url,StringLength(500)] public string? MainImageUrl { get; set; }
 public bool IsActive { get; set; } = true;
 public List<DiveCenter> Centers { get; set; } = [];
 public List<ActivityCategory> Categories { get; set; } = [];
}
public class CategoryEditViewModel
{
 public int Id { get; set; }
 [Required,StringLength(100)] public string Name { get; set; } = "";
 [Required,StringLength(100),RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")] public string Slug { get; set; } = "";
 public bool IsActive { get; set; } = true;
}
public class UserEditViewModel
{
 public int Id { get; set; }
 [Required,RegularExpression("^(Admin|DiveCenter|Customer)$")] public string Role { get; set; } = "Customer";
 public bool IsActive { get; set; }
 public string Name { get; set; } = "";
}
public class CenterUserViewModel
{
 [Range(1,int.MaxValue)] public int DiveCenterId { get; set; }
 [Range(1,int.MaxValue)] public int AppUserId { get; set; }
 public bool IsOwner { get; set; } = true;
 public List<DiveCenter> Centers { get; set; } = [];
 public List<AppUser> Users { get; set; } = [];
}
