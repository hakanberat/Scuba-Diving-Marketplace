using Dapper;
using DiveMarketplace.Data;
using DiveMarketplace.Models;
using DiveMarketplace.ViewModels;
namespace DiveMarketplace.Services;

public class AdminService(SqlConnectionFactory factory, ReservationService reservations)
{
    public async Task<AdminOverviewViewModel> GetOverviewAsync()
    {
        using var db = factory.CreateConnection();
        var model=await db.QuerySingleAsync<AdminOverviewViewModel>("""
   SELECT (SELECT COUNT(*) FROM DiveCenters) Centers,
          (SELECT COUNT(*) FROM AppUsers) Users,
          (SELECT COUNT(*) FROM AppUsers WHERE Role='DiveCenter') Owners,
          (SELECT COUNT(*) FROM Activities) Activities,
          (SELECT COUNT(*) FROM Reservations) Reservations,
          (SELECT COUNT(*) FROM DiveCenterApplications a JOIN AppUsers u ON u.Id=a.AppUserId WHERE a.Status='Pending' AND u.IsActive=1 AND u.Role='DiveCenter') PendingApplications
   """);
        model.Revenue=await reservations.GetRevenueAsync(0,true);
        return model;
    }
    public async Task<List<DiveCenter>> GetCentersAsync()
    {
        using var db = factory.CreateConnection();
        return (await db.QueryAsync<DiveCenter>("SELECT * FROM DiveCenters ORDER BY Name")).ToList();
    }
    public async Task<bool> SetCenterActiveAsync(int id, bool active)
    {
        using var db = factory.CreateConnection();
        return await db.ExecuteAsync("UPDATE DiveCenters SET IsActive=@active WHERE Id=@id", new { id, active }) == 1;
    }
}
