using Dapper;
using DiveMarketplace.Data;
using DiveMarketplace.Models;
using DiveMarketplace.ViewModels;
namespace DiveMarketplace.Services;

public class ActivityCategoryService(SqlConnectionFactory factory)
{
    public async Task<List<ActivityCategory>> GetAllAsync(bool includeInactive = false)
    {
        using var db = factory.CreateConnection();
        return (await db.QueryAsync<ActivityCategory>("SELECT * FROM ActivityCategories WHERE IsActive=1 OR @includeInactive=1 ORDER BY Name", new { includeInactive })).ToList();
    }
    public async Task<CategoryEditViewModel?> GetAsync(int id)
    {
        using var db = factory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<CategoryEditViewModel>("SELECT * FROM ActivityCategories WHERE Id=@id", new { id });
    }
    public async Task SaveAsync(CategoryEditViewModel model)
    {
        using var db = factory.CreateConnection();
        await db.ExecuteAsync(model.Id == 0 ? "INSERT ActivityCategories(Name,Slug,IsActive) VALUES(@Name,@Slug,@IsActive)" : "UPDATE ActivityCategories SET Name=@Name,Slug=@Slug,IsActive=@IsActive WHERE Id=@Id", model);
    }
}