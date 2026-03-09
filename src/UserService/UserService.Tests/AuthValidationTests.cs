using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using UserService.API.DTOs;
using UserService.API.Services;
using Xunit;

namespace UserService.Tests;

public class UserServiceApplicationFactory : WebApplicationFactory<Program>
{
    private const string JwtSecret = "super-secret-test-key-1234567890123456";
    private const string JwtIssuer = "TestIssuer";
    private const string JwtAudience = "TestAudience";

    public static readonly string TestJwtSecret = JwtSecret;
    public static readonly string TestJwtIssuer = JwtIssuer;
    public static readonly string TestJwtAudience = JwtAudience;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = JwtSecret,
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience
            };

            configBuilder.AddInMemoryCollection(settings!);
        });

        builder.ConfigureServices(services =>
        {
            // Replace IUserService with a test double that does not hit a real database
            services.AddScoped<IUserService, FakeUserService>();
        });

        return base.CreateHost(builder);
    }
}

public class AuthValidationTests : IClassFixture<UserServiceApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthValidationTests(UserServiceApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task MissingToken_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InvalidToken_Returns401()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "abcdefg");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MalformedToken_Returns401()
    {
        // Arrange
        var validToken = GenerateJwtToken(userId: 1, role: "Admin", expires: DateTime.UtcNow.AddMinutes(10));
        var malformedToken = validToken.Remove(validToken.Length / 2, 5);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", malformedToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExpiredToken_Returns401()
    {
        // Arrange
        var expiredToken = GenerateJwtToken(userId: 1, role: "Admin", expires: DateTime.UtcNow.AddMinutes(-5));

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ValidToken_Returns200()
    {
        // Arrange
        var validToken = GenerateJwtToken(userId: 1, role: "Admin", expires: DateTime.UtcNow.AddMinutes(10));

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HorizontalPrivilegeEscalation_StandardUserAccessingAnotherUsersData_Returns403()
    {
        // User with ID 1 tries to access user 2's data
        var userToken = GenerateJwtToken(userId: 1, role: "User", expires: DateTime.UtcNow.AddMinutes(10));

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/2");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerticalPrivilegeEscalation_StandardUserCallingAdminEndpoint_Returns403()
    {
        // Standard user tries to call an Admin-only endpoint
        var userToken = GenerateJwtToken(userId: 1, role: "User", expires: DateTime.UtcNow.AddMinutes(10));

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static string GenerateJwtToken(int userId, string role, DateTime? expires)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(UserServiceApplicationFactory.TestJwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "test@example.com"),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: UserServiceApplicationFactory.TestJwtIssuer,
            audience: UserServiceApplicationFactory.TestJwtAudience,
            claims: claims,
            expires: expires ?? DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

internal class FakeUserService : IUserService
{
    public Task<IEnumerable<UserResponse>> GetAllAsync() =>
        Task.FromResult<IEnumerable<UserResponse>>(Array.Empty<UserResponse>());

    public Task<UserResponse?> GetByIdAsync(int userId) =>
        Task.FromResult<UserResponse?>(null);

    public Task<UserResponse?> GetMeAsync(int userId)
    {
        // Always return a simple user for valid authenticated requests
        var user = new UserResponse(
            UserId: userId,
            FullName: "Test User",
            Email: "test@example.com",
            Role: "Admin",
            IsActive: true,
            CreatedAt: DateTime.UtcNow.AddDays(-1),
            UpdatedAt: DateTime.UtcNow);

        return Task.FromResult<UserResponse?>(user);
    }

    public Task<UserResponse?> UpdateAsync(int userId, UpdateUserRequest request) =>
        Task.FromResult<UserResponse?>(null);

    public Task<bool> SoftDeleteAsync(int userId) =>
        Task.FromResult(false);
}

