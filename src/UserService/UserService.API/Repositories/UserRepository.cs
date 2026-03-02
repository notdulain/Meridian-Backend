using Dapper;
using Microsoft.Data.SqlClient;
using UserService.API.Models;

namespace UserService.API.Repositories;

public class UserRepository : IUserRepository
{
    private readonly string _connectionString;

    public UserRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("UserDb")
            ?? throw new InvalidOperationException("ConnectionStrings:UserDb is not configured.");
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        await using var connection = new SqlConnection(_connectionString);
        return await connection.QueryAsync<User>("SELECT * FROM Users WHERE IsActive = 1");
    }

    public async Task<User?> GetByIdAsync(int userId)
    {
        await using var connection = new SqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE UserId = @UserId AND IsActive = 1",
            new { UserId = userId });
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        await using var connection = new SqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Email = @Email",
            new { Email = email });
    }

    public async Task<User> CreateAsync(User user)
    {
        await using var connection = new SqlConnection(_connectionString);
        var id = await connection.ExecuteScalarAsync<int>("""
            INSERT INTO Users (FullName, Email, PasswordHash, Role, IsActive, CreatedAt, UpdatedAt)
            VALUES (@FullName, @Email, @PasswordHash, @Role, @IsActive, @CreatedAt, @UpdatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """,
            new
            {
                user.FullName,
                user.Email,
                user.PasswordHash,
                Role = user.Role.ToString(),
                user.IsActive,
                user.CreatedAt,
                user.UpdatedAt
            });
        user.UserId = id;
        return user;
    }

    public async Task<User?> UpdateAsync(User user)
    {
        await using var connection = new SqlConnection(_connectionString);
        var rows = await connection.ExecuteAsync("""
            UPDATE Users
            SET FullName = @FullName, Email = @Email, UpdatedAt = @UpdatedAt
            WHERE UserId = @UserId AND IsActive = 1
            """,
            new { user.FullName, user.Email, user.UpdatedAt, user.UserId });
        return rows > 0 ? user : null;
    }

    public async Task<bool> SoftDeleteAsync(int userId)
    {
        await using var connection = new SqlConnection(_connectionString);
        var rows = await connection.ExecuteAsync(
            "UPDATE Users SET IsActive = 0, UpdatedAt = @UpdatedAt WHERE UserId = @UserId AND IsActive = 1",
            new { UserId = userId, UpdatedAt = DateTime.UtcNow });
        return rows > 0;
    }
}
