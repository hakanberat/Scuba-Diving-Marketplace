using Dapper;
using DiveMarketplace.Data;
using DiveMarketplace.Models;
using DiveMarketplace.ViewModels;
namespace DiveMarketplace.Services;

public class InstructorService(SqlConnectionFactory factory)
{
    public async Task<List<Instructor>> GetPublicAsync(int centerId)
    {
        using var db = factory.CreateConnection();
        return (await db.QueryAsync<Instructor>("SELECT i.* FROM Instructors i JOIN DiveCenters d ON d.Id=i.DiveCenterId WHERE i.DiveCenterId=@centerId AND i.IsActive=1 AND d.IsActive=1 ORDER BY i.FirstName", new { centerId })).ToList();
    }
    public async Task<List<Instructor>> GetManagedAsync(int userId, bool admin)
    {
        using var db = factory.CreateConnection();
        return (await db.QueryAsync<Instructor>($"SELECT i.* FROM Instructors i JOIN DiveCenters d ON d.Id=i.DiveCenterId WHERE {CenterAccessService.Scope} ORDER BY i.FirstName", new { userId, admin })).ToList();
    }
    public async Task<InstructorEditViewModel?> GetEditAsync(int id, int userId, bool admin)
    {
        using var db = factory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<InstructorEditViewModel>($"SELECT i.* FROM Instructors i JOIN DiveCenters d ON d.Id=i.DiveCenterId WHERE i.Id=@id AND {CenterAccessService.Scope}", new { id, userId, admin });
    }
    public async Task<bool> SaveAsync(InstructorEditViewModel m, int userId, bool admin)
    {
        using var db = factory.CreateConnection();
        var sql = m.Id == 0 ? $"""
   INSERT Instructors(DiveCenterId,FirstName,LastName,Bio,PhotoUrl,CertificationAgency,InstructorNumber,IsActive)
   SELECT @DiveCenterId,@FirstName,@LastName,@Bio,@PhotoUrl,@CertificationAgency,@InstructorNumber,@IsActive
   FROM DiveCenters d WHERE d.Id=@DiveCenterId AND {CenterAccessService.Scope}
   """ : $"""
   UPDATE i SET FirstName=@FirstName,LastName=@LastName,Bio=@Bio,PhotoUrl=@PhotoUrl,CertificationAgency=@CertificationAgency,InstructorNumber=@InstructorNumber,IsActive=@IsActive
   FROM Instructors i JOIN DiveCenters d ON d.Id=i.DiveCenterId WHERE i.Id=@Id AND i.DiveCenterId=@DiveCenterId AND {CenterAccessService.Scope}
   """;
        var parameters = new DynamicParameters(m); parameters.Add("userId", userId); parameters.Add("admin", admin);
        return await db.ExecuteAsync(sql, parameters) == 1;
    }
    public async Task<bool> SetActiveAsync(int id, bool active, int userId, bool admin)
    {
        using var db = factory.CreateConnection();
        return await db.ExecuteAsync($"UPDATE i SET IsActive=@active FROM Instructors i JOIN DiveCenters d ON d.Id=i.DiveCenterId WHERE i.Id=@id AND {CenterAccessService.Scope}", new { id, active, userId, admin }) == 1;
    }
}