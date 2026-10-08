namespace DiveMarketplace.Models;

public class ActivityCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
public class Instructor
{
    public int Id { get; set; }
    public int DiveCenterId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Bio { get; set; }
    public string? PhotoUrl { get; set; }
    public string? CertificationAgency { get; set; }
    public string? InstructorNumber { get; set; }
    public bool IsActive { get; set; } = true;
}
public class DiveActivity
{
    public int Id { get; set; }
    public int DiveCenterId { get; set; }
    public int ActivityCategoryId { get; set; }
    public string Name { get; set; } = "";
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Location { get; set; }
    public string? MapLocation { get; set; }
    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = "EUR";
    public string? Duration { get; set; }
    public int? MinimumAge { get; set; }
    public string? MinimumCertification { get; set; }
    public int? MaxParticipants { get; set; }
    public string? Included { get; set; }
    public string? NotIncluded { get; set; }
    public string? Requirements { get; set; }
    public string? MainImageUrl { get; set; }
    public string? CoverPhotoUrl { get; set; }
    public bool UsesRepresentativeImage => string.IsNullOrWhiteSpace(CoverPhotoUrl) && string.IsNullOrWhiteSpace(MainImageUrl);
    public string? DisplayImageUrl => !string.IsNullOrWhiteSpace(CoverPhotoUrl) ? CoverPhotoUrl : !string.IsNullOrWhiteSpace(MainImageUrl) ? MainImageUrl : MarketplaceImages.ForActivity(Id, CategoryName);
    public bool IsActive { get; set; } = true;
    public string CategoryName { get; set; } = "";
    public string DiveCenterName { get; set; } = "";
}
public class Reservation
{
    public int Id { get; set; }
    public string ReservationNumber { get; set; } = "";
    public int ActivityId { get; set; }
    public int DiveCenterId { get; set; }
    public int CustomerUserId { get; set; }
    public DateTime ReservationDate { get; set; }
    public int Participants { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string CurrencyCode { get; set; } = "";
    public string Status { get; set; } = "";
    public string? CustomerNotes { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactWhatsApp { get; set; }
    public bool ContactByEmail { get; set; }
    public bool ContactByPhone { get; set; }
    public bool ContactByWhatsApp { get; set; }
    public string? DiveCenterNotes { get; set; }
    public string ActivityName { get; set; } = "";
    public string DiveCenterName { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}
