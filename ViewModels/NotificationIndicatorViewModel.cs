namespace DiveMarketplace.ViewModels;

public class NotificationIndicatorViewModel
{
    public int UserId { get; set; }
    public int UnreadCount { get; set; }
    public int LatestUnreadId { get; set; }
    public int PendingApplications { get; set; }
}
