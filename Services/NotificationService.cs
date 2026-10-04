using System.Data;
using Dapper;
using DiveMarketplace.Data;
using DiveMarketplace.Models;
using DiveMarketplace.ViewModels;
namespace DiveMarketplace.Services;
public class NotificationService(SqlConnectionFactory factory)
{
    public async Task<NotificationIndicatorViewModel> GetIndicatorAsync(int userId,bool admin=false)
    {
        using var db=factory.CreateConnection();
        var model=await db.QuerySingleAsync<NotificationIndicatorViewModel>("""
            SELECT COUNT(*) UnreadCount,COALESCE(MAX(Id),0) LatestUnreadId,
             (SELECT COUNT(*) FROM DiveCenterApplications a JOIN AppUsers u ON u.Id=a.AppUserId
              WHERE @admin=1 AND a.Status='Pending' AND u.IsActive=1 AND u.Role='DiveCenter') PendingApplications
            FROM Notifications WHERE AppUserId=@userId AND IsRead=0
            """,new {userId,admin});
        model.UserId=userId;
        return model;
    }
    public async Task<List<Notification>> GetAsync(int userId,bool history=false)
    {
        using var db = factory.CreateConnection();
        return (await db.QueryAsync<Notification>("SELECT TOP(100) * FROM Notifications WHERE AppUserId=@userId AND (@history=1 OR IsRead=0) ORDER BY Id DESC",new {userId,history})).ToList();
    }
    public async Task<int> UnreadCountAsync(int userId)
    {
        using var db = factory.CreateConnection();
        return await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Notifications WHERE AppUserId=@userId AND IsRead=0",new {userId});
    }
    public async Task<bool> MarkReadAsync(int id,int userId)
    {
        using var db = factory.CreateConnection();
        return await db.ExecuteAsync("UPDATE Notifications SET IsRead=1 WHERE Id=@id AND AppUserId=@userId",new {id,userId})==1;
    }
    public async Task MarkAllReadAsync(int userId)
    {
        using var db = factory.CreateConnection();
        await db.ExecuteAsync("UPDATE Notifications SET IsRead=1 WHERE AppUserId=@userId AND IsRead=0",new {userId});
    }
    public static Task NotifyAdminsAsync(IDbConnection db,IDbTransaction tx,int applicationId,string centerName) => db.ExecuteAsync("""
        INSERT Notifications(AppUserId,EventKey,Title,Message,LinkUrl)
        SELECT Id,@key,N'New dive center application',@message,@link FROM AppUsers WHERE Role='Admin' AND IsActive=1
        """,new {key=$"ApplicationSubmitted:{applicationId}",message=$"{centerName} submitted an owner application for review.",link=$"/Admin/Application/{applicationId}"},tx);
    public static Task NotifyApplicantAsync(IDbConnection db,IDbTransaction tx,int applicationId,int userId,string status) => db.ExecuteAsync("""
        INSERT Notifications(AppUserId,EventKey,Title,Message,LinkUrl) VALUES(@userId,@key,N'Dive center application reviewed',@message,'/Account/ApplicationStatus')
        """,new {userId,key=$"ApplicationReviewed:{applicationId}",message=$"Your dive center application was {status.ToLowerInvariant()}. Open your application to see the details."},tx);
    public static Task NotifyReservationOwnersAsync(IDbConnection db,IDbTransaction tx,int reservationId) => db.ExecuteAsync("""
        INSERT Notifications(AppUserId,EventKey,Title,Message,LinkUrl)
        SELECT u.Id,@key,N'New reservation request',a.Name+N' has a new reservation request.',@link
        FROM Reservations r JOIN Activities a ON a.Id=r.ActivityId JOIN DiveCenterUsers m ON m.DiveCenterId=r.DiveCenterId
        JOIN AppUsers u ON u.Id=m.AppUserId WHERE r.Id=@reservationId AND m.IsOwner=1 AND u.Role='DiveCenter' AND u.IsActive=1
        """,new {reservationId,key=$"ReservationCreated:{reservationId}",link=$"/Reservations/Details/{reservationId}"},tx);
    public static Task NotifyReservationCustomerAsync(IDbConnection db,IDbTransaction tx,int reservationId,string status) => db.ExecuteAsync("""
        INSERT Notifications(AppUserId,EventKey,Title,Message,LinkUrl)
        SELECT CustomerUserId,@key,N'Reservation status updated',@message,@link FROM Reservations WHERE Id=@reservationId
        """,new {reservationId,key=$"ReservationStatus:{reservationId}:{status}",message=$"Your reservation is now {status.ToLowerInvariant()}. Open the reservation for details.",link=$"/Reservations/Details/{reservationId}"},tx);
}
