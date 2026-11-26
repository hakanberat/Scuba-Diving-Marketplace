using Dapper;
using DiveMarketplace.Data;
using DiveMarketplace.Models;
using DiveMarketplace.ViewModels;
namespace DiveMarketplace.Services;

public class ReservationService(SqlConnectionFactory factory)
{
    private const string Select = "SELECT r.*,a.Name ActivityName,d.Name DiveCenterName,u.FirstName+' '+u.LastName CustomerName FROM Reservations r JOIN Activities a ON a.Id=r.ActivityId JOIN DiveCenters d ON d.Id=r.DiveCenterId JOIN AppUsers u ON u.Id=r.CustomerUserId ";
    public async Task<int?> CreateAsync(ReservationCreateViewModel m, int userId)
    {
        if (m.ReservationDate.Date < DateTime.Today || m.Participants < 1 || m.Participants > 10000) return null;
        if(!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(m,new(m),[],true)) return null;
        using var db = factory.CreateConnection();
        await db.OpenAsync();
        await using var transaction=await db.BeginTransactionAsync();
        // A single INSERT SELECT validates availability and snapshots the authoritative current price.
        var id = await db.QuerySingleOrDefaultAsync<int?>("""
   INSERT Reservations(ReservationNumber,ActivityId,DiveCenterId,CustomerUserId,ReservationDate,Participants,UnitPrice,CurrencyCode,TotalPrice,CustomerNotes,ContactEmail,ContactPhone,ContactWhatsApp,ContactByEmail,ContactByPhone,ContactByWhatsApp)
   OUTPUT INSERTED.Id
   SELECT @number,a.Id,a.DiveCenterId,@userId,@date,@participants,a.Price,a.CurrencyCode,a.Price*@participants,@notes,@ContactEmail,@ContactPhone,@ContactWhatsApp,@ContactByEmail,@ContactByPhone,@ContactByWhatsApp
   FROM Activities a JOIN DiveCenters d ON d.Id=a.DiveCenterId JOIN ActivityCategories c ON c.Id=a.ActivityCategoryId
   WHERE a.Id=@activityId AND a.IsActive=1 AND d.IsActive=1 AND c.IsActive=1
   AND (a.MaxParticipants IS NULL OR @participants<=a.MaxParticipants)
   AND EXISTS(SELECT 1 FROM AppUsers WHERE Id=@userId AND IsActive=1 AND Role='Customer')
   """, new { number = $"DM-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}", userId, date = m.ReservationDate.Date, participants = m.Participants, notes = m.CustomerNotes, activityId = m.ActivityId,ContactEmail=m.ContactEmail.Trim(),ContactPhone=m.ContactPhone.Trim(),ContactWhatsApp=m.ContactWhatsApp?.Trim(),m.ContactByEmail,m.ContactByPhone,m.ContactByWhatsApp },transaction);
        if(id is null) return null;
        await NotificationService.NotifyReservationOwnersAsync(db,transaction,id.Value);
        await transaction.CommitAsync();
        return id;
    }
    public async Task<List<Reservation>> GetCustomerAsync(int userId)
    {
        using var db = factory.CreateConnection();
        return (await db.QueryAsync<Reservation>(Select + "WHERE r.CustomerUserId=@userId ORDER BY r.CreatedAt DESC", new { userId })).ToList();
    }
    public async Task<List<Reservation>> GetManagedAsync(int userId, bool admin, string? status = null)
    {
        using var db = factory.CreateConnection();
        return (await db.QueryAsync<Reservation>(Select + $"WHERE {CenterAccessService.Scope} AND (@status IS NULL OR r.Status=@status) ORDER BY r.CreatedAt DESC", new { userId, admin, status })).ToList();
    }
    public async Task<Reservation?> GetAsync(int id, int userId, bool admin, bool manager)
    {
        using var db = factory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<Reservation>(Select + $"WHERE r.Id=@id AND (@admin=1 OR r.CustomerUserId=@userId OR (@manager=1 AND {CenterAccessService.Scope}))", new { id, userId, admin, manager });
    }
    public async Task<List<ReservationRevenueViewModel>> GetRevenueAsync(int userId, bool admin)
    {
        using var db=factory.CreateConnection();
        return (await db.QueryAsync<ReservationRevenueViewModel>($"""
            SELECT d.Id DiveCenterId,d.Name CenterName,r.CurrencyCode,
                SUM(CASE WHEN r.Status='Confirmed' THEN 1 ELSE 0 END) ConfirmedCount,
                SUM(CASE WHEN r.Status='Confirmed' THEN r.TotalPrice ELSE 0 END) ConfirmedAmount,
                SUM(CASE WHEN r.Status='Pending' THEN r.TotalPrice ELSE 0 END) PendingAmount
            FROM Reservations r JOIN DiveCenters d ON d.Id=r.DiveCenterId
            WHERE {CenterAccessService.Scope}
            GROUP BY d.Id,d.Name,r.CurrencyCode
            ORDER BY d.Name,d.Id,r.CurrencyCode
            """,new {userId,admin})).ToList();
    }
    public async Task<bool> ChangeStatusAsync(int id, string status, string? notes, int userId, bool admin, bool manager)
    {
        if (status is not ("Confirmed" or "Rejected" or "Cancelled") || notes?.Length > 2000) return false;
        using var db = factory.CreateConnection();
        await db.OpenAsync();
        await using var transaction=await db.BeginTransactionAsync();
        var changed = await db.ExecuteAsync($"""
   UPDATE r SET Status=@status,UpdatedAt=SYSUTCDATETIME(),
   ConfirmedAt=CASE WHEN @status='Confirmed' THEN SYSUTCDATETIME() ELSE ConfirmedAt END,
   CancelledAt=CASE WHEN @status='Cancelled' THEN SYSUTCDATETIME() ELSE CancelledAt END,
   DiveCenterNotes=CASE WHEN @manager=1 OR @admin=1 THEN @notes ELSE DiveCenterNotes END
   FROM Reservations r JOIN DiveCenters d ON d.Id=r.DiveCenterId
   WHERE r.Id=@id AND
   ((@status IN ('Confirmed','Rejected') AND r.Status='Pending') OR (@status='Cancelled' AND r.Status IN ('Pending','Confirmed')))
   AND ( (@manager=1 AND {CenterAccessService.Scope}) OR @admin=1 OR (@status='Cancelled' AND r.CustomerUserId=@userId))
   """, new { id, status, notes, userId, admin, manager },transaction) == 1;
        if(!changed) return false;
        if(manager || admin) await NotificationService.NotifyReservationCustomerAsync(db,transaction,id,status);
        await transaction.CommitAsync();
        return true;
    }
}
