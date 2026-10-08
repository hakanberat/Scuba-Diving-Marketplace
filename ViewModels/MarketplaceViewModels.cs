using System.ComponentModel.DataAnnotations;
using DiveMarketplace.Models;
namespace DiveMarketplace.ViewModels;
public class ActivitySearchViewModel
{
 [StringLength(200)] public string? Search { get; set; }
 [StringLength(100)] public string? Country { get; set; }
 [StringLength(100)] public string? City { get; set; }
 public int? CategoryId { get; set; }
 [Range(0,100000000)] public decimal? MinPrice { get; set; }
 [Range(0,100000000)] public decimal? MaxPrice { get; set; }
 public List<DiveActivity> Activities { get; set; } = [];
 public List<ActivityCategory> Categories { get; set; } = [];
}
public class DiveCenterDetailsViewModel
{
 public List<Photo> Photos { get; set; } = [];
 public DiveCenter Center { get; set; } = new();
 public List<Instructor> Instructors { get; set; } = [];
 public List<DiveActivity> Activities { get; set; } = [];
}
public class HomeViewModel
{
 public Dictionary<int, string?> CategoryImages { get; set; } = [];
 public Dictionary<int, int> CategoryActivityCounts { get; set; } = [];
 public List<DestinationViewModel> Destinations { get; set; } = [];
 public List<DiveActivity> Activities { get; set; } = [];
 public List<ActivityCategory> Categories { get; set; } = [];
 public List<DiveCenter> Centers { get; set; } = [];
}
public class DestinationViewModel
{
 public string Country { get; set; } = "";
 public string? City { get; set; }
 public string? ImageUrl { get; set; }
}
public class ReservationCreateViewModel : IValidatableObject
{
 public int ActivityId { get; set; }
 [Required, DataType(DataType.Date)] public DateTime ReservationDate { get; set; } = DateTime.Today.AddDays(1);
 [Range(1,10000)] public int Participants { get; set; } = 1;
 [StringLength(2000)] public string? CustomerNotes { get; set; }
 [Required,EmailAddress,StringLength(200)] public string ContactEmail { get; set; } = "";
 [Required,Phone,StringLength(50)] public string ContactPhone { get; set; } = "";
 [Phone,StringLength(50)] public string? ContactWhatsApp { get; set; }
 public bool ContactByEmail { get; set; } = true;
 public bool ContactByPhone { get; set; }
 public bool ContactByWhatsApp { get; set; }
 public DiveActivity? Activity { get; set; }
 public IEnumerable<ValidationResult> Validate(ValidationContext context)
 {
  if(!ContactByEmail && !ContactByPhone && !ContactByWhatsApp)
   yield return new ValidationResult("Select at least one preferred contact method.");
  if(ContactByWhatsApp && string.IsNullOrWhiteSpace(ContactWhatsApp))
   yield return new ValidationResult("Enter your WhatsApp number when selecting WhatsApp.",[nameof(ContactWhatsApp)]);
  foreach(var number in new[]{(nameof(ContactPhone),ContactPhone),(nameof(ContactWhatsApp),ContactWhatsApp)})
  {
   if(!string.IsNullOrWhiteSpace(number.Item2))
   {
    var digits=number.Item2.Count(char.IsAsciiDigit);
    if(digits<7 || digits>15)
     yield return new ValidationResult("Enter a phone number with 7-15 digits, including its country code.",[number.Item1]);
   }
  }
 }
}
public class DashboardViewModel
{
 public List<ReservationRevenueViewModel> Revenue { get; set; } = [];
 public int ActiveActivities { get; set; }
 public int Instructors { get; set; }
 public int Pending { get; set; }
 public int Confirmed { get; set; }
 public int Upcoming { get; set; }
 public List<Reservation> LatestReservations { get; set; } = [];
 public List<DiveCenter> Centers { get; set; } = [];
}
public class AdminOverviewViewModel
{
 public List<ReservationRevenueViewModel> Revenue { get; set; } = [];
 public int Owners { get; set; }
 public int PendingApplications { get; set; }
 public int Centers { get; set; }
 public int Users { get; set; }
 public int Activities { get; set; }
 public int Reservations { get; set; }
}
