using Dapper;
using Microsoft.Data.SqlClient;
using UserService.API.Models;

namespace UserService.API.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly string _connectionString;

    public RefreshTokenRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("UserDb")
            ?? throw new InvalidOperationException("ConnectionStrings:UserDb is not configured.");
    }

    public async Task<RefreshToken> CreateAsync(RefreshToken refreshToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        var id = await connection.ExecuteScalarAsync<int>("""
            INSERT INTO RefreshTokens (UserId, Token, ExpiresAt, IsRevoked, CreatedAt)
            VALUES (@UserId, @Token, @ExpiresAt, @IsRevoked, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """,
            new
            {
                refreshToken.UserId,
                refreshToken.Token,
                refreshToken.ExpiresAt,
                refreshToken.IsRevoked,
                refreshToken.CreatedAt
            });
        refreshToken.RefreshTokenId = id;
        return refreshToken;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        await using var connection = new SqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<RefreshToken>(
            "SELECT * FROM RefreshTokens WHERE Token = @Token",
            new { Token = token });
    }

    public async Task<bool> RevokeAsync(string token)
    {
        await using var connection = new SqlConnection(_connectionString);
        var rows = await connection.ExecuteAsync(
            "UPDATE RefreshTokens SET IsRevoked = 1 WHERE Token = @Token AND IsRevoked = 0",
            new { Token = token });
        return rows > 0;
    }
}
