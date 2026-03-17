using Dapper;
using MySqlConnector;
using AssignmentService.API.Models;

namespace AssignmentService.API.Repositories;

public class AssignmentRepository : IAssignmentRepository
{
    private readonly string _connectionString;

    public AssignmentRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("AssignmentDb")
            ?? throw new InvalidOperationException("AssignmentDb connection string is not configured.");
    }

    private MySqlConnection CreateConnection() => new(_connectionString);

    public async Task<Assignment> CreateAsync(Assignment assignment)
    {
        const string sql = @"
            INSERT INTO Assignments (DeliveryId, VehicleId, DriverId, AssignedAt, AssignedBy, Status, Notes, CreatedAt, UpdatedAt)
            VALUES (@DeliveryId, @VehicleId, @DriverId, @AssignedAt, @AssignedBy, @Status, @Notes, @CreatedAt, @UpdatedAt);
            SELECT LAST_INSERT_ID();";

        using var conn = CreateConnection();
        var id = await conn.ExecuteScalarAsync<int>(sql, assignment);
        assignment.AssignmentId = id;
        return assignment;
    }

    public async Task<Assignment?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Assignments WHERE AssignmentId = @id";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Assignment>(sql, new { id });
    }

    public async Task<(IEnumerable<Assignment> Items, int TotalCount)> GetAllAsync(int page, int pageSize)
    {
        const string sql = @"
            SELECT * FROM Assignments ORDER BY CreatedAt DESC LIMIT @pageSize OFFSET @offset;
            SELECT COUNT(*) FROM Assignments;";

        using var conn = CreateConnection();
        using var multi = await conn.QueryMultipleAsync(sql, new { pageSize, offset = (page - 1) * pageSize });

        var items = await multi.ReadAsync<Assignment>();
        var totalCount = await multi.ReadFirstAsync<int>();
        return (items, totalCount);
    }

    public async Task<Assignment?> GetByDeliveryIdAsync(int deliveryId)
    {
        const string sql = "SELECT * FROM Assignments WHERE DeliveryId = @deliveryId ORDER BY CreatedAt DESC LIMIT 1";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Assignment>(sql, new { deliveryId });
    }

    public async Task<Assignment?> GetActiveByDriverIdAsync(int driverId)
    {
        const string sql = "SELECT * FROM Assignments WHERE DriverId = @driverId AND Status = 'Active' LIMIT 1";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Assignment>(sql, new { driverId });
    }

    public async Task<bool> UpdateStatusAsync(int id, string status)
    {
        const string sql = "UPDATE Assignments SET Status = @status, UpdatedAt = @updatedAt WHERE AssignmentId = @id";
        using var conn = CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { id, status, updatedAt = DateTime.UtcNow });
        return rows > 0;
    }
}
