using Microsoft.Extensions.Configuration;
using Moq;
using UserService.API.DTOs;
using UserService.API.Models;
using UserService.API.Repositories;
using UserService.API.Services;
using Xunit;

namespace UserService.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly IConfiguration _configuration;

    public AuthServiceTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "super-secret-test-key-1234567890123456",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience"
            })
            .Build();
    }

    [Fact]
    public async Task RegisterAsync_RejectsDriverRoleForPublicSignup()
    {
        var service = new AuthService(
            _userRepositoryMock.Object,
            _refreshTokenRepositoryMock.Object,
            _configuration);

        var request = new RegisterRequest(
            FullName: "Driver User",
            Email: "driver@example.com",
            Password: "Password123!",
            Role: UserRole.Driver.ToString());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(request));

        Assert.Equal("Driver accounts must be created by an admin.", ex.Message);
        _userRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
    }
}
