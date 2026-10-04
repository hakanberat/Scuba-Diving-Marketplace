using Dapper;
using DiveMarketplace.Data;
using DiveMarketplace.Models;

namespace DiveMarketplace.Services;

public class DiveCenterService
{
    private readonly SqlConnectionFactory _connectionFactory;

    public DiveCenterService(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<DiveCenter?> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT
                Id,
                Name,
                Description,
                Country,
                City,
                Address,
                MapLocation,
                Phone,
                Email,
                Website,
                LogoUrl,
                IsActive,
                CreatedAt,
                (SELECT TOP(1) p.ImageUrl FROM Photos p WHERE p.DiveCenterId=DiveCenters.Id ORDER BY p.IsCover DESC,p.Id) CoverPhotoUrl
            FROM DiveCenters
            WHERE Id = @Id AND IsActive = 1
            """;

        using var connection = _connectionFactory.CreateConnection();

        return await connection.QuerySingleOrDefaultAsync<DiveCenter>(sql, new { Id = id });
    }

    public async Task<IEnumerable<DiveCenter>> GetAllAsync()
    {
        const string sql = """
            SELECT
                Id,
                Name,
                Description,
                Country,
                City,
                Address,
                MapLocation,
                Phone,
                Email,
                Website,
                LogoUrl,
                IsActive,
                CreatedAt,
                (SELECT TOP(1) p.ImageUrl FROM Photos p WHERE p.DiveCenterId=DiveCenters.Id ORDER BY p.IsCover DESC,p.Id) CoverPhotoUrl
            FROM DiveCenters
            WHERE IsActive = 1
            ORDER BY Name
            """;

        using var connection = _connectionFactory.CreateConnection();

        return await connection.QueryAsync<DiveCenter>(sql);
    }
}
