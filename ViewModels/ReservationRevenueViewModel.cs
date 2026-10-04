namespace DiveMarketplace.ViewModels;

public class ReservationRevenueViewModel
{
    public int DiveCenterId { get; set; }
    public string CenterName { get; set; } = "";
    public string CurrencyCode { get; set; } = "";
    public int ConfirmedCount { get; set; }
    public decimal ConfirmedAmount { get; set; }
    public decimal PendingAmount { get; set; }
}
