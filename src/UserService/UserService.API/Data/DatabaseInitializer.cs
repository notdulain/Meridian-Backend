using MySqlConnector;

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
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();

        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Users (
                UserId       INT              NOT NULL AUTO_INCREMENT,
                FullName     VARCHAR(255)     NOT NULL,
                Email        VARCHAR(255)     NOT NULL UNIQUE,
                PasswordHash VARCHAR(512)     NOT NULL,
                Role         ENUM('Admin','Dispatcher','Driver') NOT NULL,
                IsActive     TINYINT(1)       NOT NULL DEFAULT 1,
                CreatedAt    DATETIME         NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt    DATETIME         NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                PRIMARY KEY (UserId)
            );
            """;
        await cmd.ExecuteNonQueryAsync();
        _logger.LogInformation("Users table ensured.");

        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS RefreshTokens (
                RefreshTokenId INT          NOT NULL AUTO_INCREMENT,
                UserId         INT          NOT NULL,
                Token          VARCHAR(512) NOT NULL UNIQUE,
                ExpiresAt      DATETIME     NOT NULL,
                IsRevoked      TINYINT(1)   NOT NULL DEFAULT 0,
                CreatedAt      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (RefreshTokenId),
                CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
            );
            """;
        await cmd.ExecuteNonQueryAsync();
        _logger.LogInformation("RefreshTokens table ensured.");
    }
}
