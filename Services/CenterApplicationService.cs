using Dapper;
using DiveMarketplace.Data;
using DiveMarketplace.Models;
using DiveMarketplace.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
namespace DiveMarketplace.Services;
public class CenterApplicationService(SqlConnectionFactory factory,IPasswordHasher<AppUser> hasher)
{
    private const string Select="SELECT a.*,u.Email,u.FirstName+' '+u.LastName OwnerName FROM DiveCenterApplications a JOIN AppUsers u ON u.Id=a.AppUserId ";
    public async Task<AppUser?> RegisterAsync(OwnerRegistrationViewModel model)
    {
        var user=new AppUser {FirstName=model.FirstName.Trim(),LastName=model.LastName.Trim(),Email=model.Email.Trim().ToLowerInvariant(),Phone=model.Phone,Role="DiveCenter",IsActive=true};
        user.PasswordHash=hasher.HashPassword(user,model.Password);
        using var db=factory.CreateConnection(); await db.OpenAsync();
        await using var tx=await db.BeginTransactionAsync();
        try
        {
            user.Id=await db.QuerySingleAsync<int>("INSERT AppUsers(FirstName,LastName,Email,PasswordHash,Role,Phone) OUTPUT INSERTED.Id VALUES(@FirstName,@LastName,@Email,@PasswordHash,'DiveCenter',@Phone)",user,tx);
            var applicationId=await db.QuerySingleAsync<int>("""
                INSERT DiveCenterApplications(AppUserId,Name,Description,Country,City,Address,Phone,Website,MapLocation)
                OUTPUT INSERTED.Id VALUES(@userId,@name,@Description,@Country,@City,@Address,@Phone,@Website,@MapLocation)
                """,new {userId=user.Id,name=model.CenterName.Trim(),model.Description,model.Country,model.City,model.Address,model.Phone,Website=WebAddressAttribute.Normalize(model.Website),model.MapLocation},tx);
            await NotificationService.NotifyAdminsAsync(db,tx,applicationId,model.CenterName.Trim());
            await tx.CommitAsync(); return user;
        }
        catch(SqlException ex) when(ex.Number is 2601 or 2627) { return null; }
    }
    public async Task<List<CenterApplicationViewModel>> GetAllAsync()
    {
        using var db=factory.CreateConnection();
        return (await db.QueryAsync<CenterApplicationViewModel>(Select+"ORDER BY CASE WHEN a.Status='Pending' THEN 0 ELSE 1 END,a.CreatedAt DESC")).ToList();
    }
    public async Task<CenterApplicationViewModel?> GetAsync(int id)
    {
        using var db=factory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<CenterApplicationViewModel>(Select+"WHERE a.Id=@id",new {id});
    }
    public async Task<CenterApplicationViewModel?> GetForUserAsync(int userId)
    {
        using var db=factory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<CenterApplicationViewModel>(Select+"WHERE a.AppUserId=@userId",new {userId});
    }
    public async Task<bool> ReviewAsync(int id,bool approve,string? notes,int adminId)
    {
        if(notes?.Length>2000) return false;
        using var db=factory.CreateConnection(); await db.OpenAsync();
        await using var tx=await db.BeginTransactionAsync();
        var application=await db.QuerySingleOrDefaultAsync<CenterApplicationViewModel>("SELECT a.* FROM DiveCenterApplications a WITH (UPDLOCK,HOLDLOCK) JOIN AppUsers u ON u.Id=a.AppUserId WHERE a.Id=@id AND a.Status='Pending' AND u.IsActive=1 AND u.Role='DiveCenter'",new {id},tx);
        if(application is null) return false;
        int? centerId=null;
        if(approve)
        {
            centerId=await db.QuerySingleAsync<int>("""
                INSERT DiveCenters(Name,Description,Country,City,Address,Phone,Email,Website,MapLocation,IsActive,CreatedAt)
                OUTPUT INSERTED.Id SELECT a.Name,a.Description,a.Country,a.City,a.Address,a.Phone,u.Email,a.Website,a.MapLocation,1,SYSUTCDATETIME()
                FROM DiveCenterApplications a JOIN AppUsers u ON u.Id=a.AppUserId WHERE a.Id=@id
                """,new {id},tx);
            await db.ExecuteAsync("INSERT DiveCenterUsers(DiveCenterId,AppUserId,IsOwner) VALUES(@centerId,@userId,1)",new {centerId,userId=application.AppUserId},tx);
        }
        var status=approve?"Approved":"Rejected";
        await db.ExecuteAsync("UPDATE DiveCenterApplications SET Status=@status,ReviewNotes=@notes,DiveCenterId=@centerId,ReviewedAt=SYSUTCDATETIME(),ReviewedByUserId=@adminId WHERE Id=@id",new {status,notes,centerId,adminId,id},tx);
        await NotificationService.NotifyApplicantAsync(db,tx,id,application.AppUserId,status);
        await tx.CommitAsync(); return true;
    }
}
