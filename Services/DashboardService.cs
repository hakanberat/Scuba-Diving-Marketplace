using DiveMarketplace.ViewModels;
namespace DiveMarketplace.Services;

public class DashboardService(ActivityService activities, InstructorService instructors, ReservationService reservations, CenterAccessService access)
{
    public async Task<DashboardViewModel> GetAsync(int userId, bool admin)
    {
        var allActivities = await activities.GetManagedAsync(userId, admin);
        var allInstructors = await instructors.GetManagedAsync(userId, admin);
        var allReservations = await reservations.GetManagedAsync(userId, admin);
        return new DashboardViewModel
        {
            Revenue = await reservations.GetRevenueAsync(userId, admin),
            ActiveActivities = allActivities.Count(x => x.IsActive),
            Instructors = allInstructors.Count(x => x.IsActive),
            Pending = allReservations.Count(x => x.Status == "Pending"),
            Confirmed = allReservations.Count(x => x.Status == "Confirmed"),
            Upcoming = allReservations.Count(x => x.Status == "Confirmed" && x.ReservationDate >= DateTime.Today),
            LatestReservations = allReservations.Take(10).ToList(),
            Centers = await access.GetCentersAsync(userId, admin)
        };
    }
}
