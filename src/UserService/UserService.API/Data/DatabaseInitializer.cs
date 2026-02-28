using Microsoft.Data.SqlClient;

namespace UserService.API.Data;

public class DatabaseInitializer
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IConfiguration configuration, ILogger<DatabaseInitializer> logger)
    {
        _connectionString = configuration.GetConnectionString("UserDb")
            ?? throw new InvalidOperationException("ConnectionStrings:UserDb is not configured.");
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();

        cmd.CommandText = """
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users')
            CREATE TABLE Users (
                UserId       INT           NOT NULL IDENTITY(1,1),
                FullName     NVARCHAR(255) NOT NULL,
                Email        NVARCHAR(255) NOT NULL,
                PasswordHash NVARCHAR(512) NOT NULL,
                Role         NVARCHAR(20)  NOT NULL,
                IsActive     BIT           NOT NULL DEFAULT 1,
                CreatedAt    DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
                UpdatedAt    DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
                CONSTRAINT PK_Users PRIMARY KEY (UserId),
                CONSTRAINT UQ_Users_Email UNIQUE (Email)
            );
            """;
        await cmd.ExecuteNonQueryAsync();
        _logger.LogInformation("Users table ensured.");

        cmd.CommandText = """
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RefreshTokens')
            CREATE TABLE RefreshTokens (
                RefreshTokenId INT           NOT NULL IDENTITY(1,1),
                UserId         INT           NOT NULL,
                Token          NVARCHAR(512) NOT NULL,
                ExpiresAt      DATETIME2     NOT NULL,
                IsRevoked      BIT           NOT NULL DEFAULT 0,
                CreatedAt      DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
                CONSTRAINT PK_RefreshTokens PRIMARY KEY (RefreshTokenId),
                CONSTRAINT UQ_RefreshTokens_Token UNIQUE (Token),
                CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
            );
            """;
        await cmd.ExecuteNonQueryAsync();
        _logger.LogInformation("RefreshTokens table ensured.");
    }
}
