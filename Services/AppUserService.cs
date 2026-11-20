using Dapper;
using DiveMarketplace.Data;
using DiveMarketplace.Models;
using DiveMarketplace.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
namespace DiveMarketplace.Services;

public class AppUserService(SqlConnectionFactory factory, IPasswordHasher<AppUser> hasher)
{
    public async Task<bool> SaveProfileAsync(int userId, ProfileViewModel model)
    {
        using var db=factory.CreateConnection();
        try {
            return await db.ExecuteAsync("UPDATE AppUsers SET FirstName=@first,LastName=@last,Email=@email,Phone=@phone WHERE Id=@userId AND IsActive=1",
                new {userId,first=model.FirstName.Trim(),last=model.LastName.Trim(),email=model.Email.Trim().ToLowerInvariant(),phone=model.Phone?.Trim()})==1;
        }
        catch(SqlException ex) when(ex.Number is 2601 or 2627) { return false; }
    }
    public async Task<AppUser?> GetByIdAsync(int id)
    {
        using var db = factory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<AppUser>("SELECT * FROM AppUsers WHERE Id=@id", new { id });
    }
    public async Task<AppUser?> AuthenticateAsync(string email, string password)
    {
        using var db = factory.CreateConnection();
        var user = await db.QuerySingleOrDefaultAsync<AppUser>("SELECT * FROM AppUsers WHERE Email=@email AND IsActive=1", new { email = email.Trim().ToLowerInvariant() });
        if (user is null) return null;
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed) return null;
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            await db.ExecuteAsync("UPDATE AppUsers SET PasswordHash=@hash WHERE Id=@id", new { hash = hasher.HashPassword(user, password), id = user.Id });
        return user;
    }
    public async Task<AppUser?> RegisterAsync(RegisterViewModel model)
    {
        var user = new AppUser { FirstName = model.FirstName.Trim(), LastName = model.LastName.Trim(), Email = model.Email.Trim().ToLowerInvariant(), Phone = model.Phone, IsActive = true };
        user.PasswordHash = hasher.HashPassword(user, model.Password);
        using var db = factory.CreateConnection();
        try
        {
            user.Id = await db.QuerySingleAsync<int>("INSERT AppUsers(FirstName,LastName,Email,PasswordHash,Role,Phone) OUTPUT INSERTED.Id VALUES(@FirstName,@LastName,@Email,@PasswordHash,'Customer',@Phone)", user);
            return user;
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627) { return null; }
    }
    public async Task<List<AppUser>> GetAllAsync()
    {
        using var db = factory.CreateConnection();
        return (await db.QueryAsync<AppUser>("SELECT Id,FirstName,LastName,Email,Role,Phone,IsActive FROM AppUsers ORDER BY Email")).ToList();
    }
    public async Task<bool> UpdateAsync(UserEditViewModel model, int currentUserId)
    {
        using var db = factory.CreateConnection();
        // Prevent an administrator from locking their own account out.
        return await db.ExecuteAsync("UPDATE AppUsers SET Role=@Role,IsActive=@IsActive WHERE Id=@Id AND Id<>@currentUserId", new { model.Role, model.IsActive, model.Id, currentUserId }) == 1;
    }
}
