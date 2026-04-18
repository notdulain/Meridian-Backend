using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UserService.API.Controllers;
using UserService.API.DTOs;
using UserService.API.Services;
using Xunit;

namespace UserService.Tests;

public class AuthControllerTests
{
    [Fact]
    public async Task Login_InvalidCredentials_Returns401ProblemDetails()
    {
        var controller = new AuthController(new FakeAuthService
        {
            LoginHandler = _ => throw new UnauthorizedAccessException("Invalid email or password.")
        });

        var result = await controller.Login(new LoginRequest("missing@example.com", "wrong"));

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(unauthorized.Value);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
        Assert.Equal(StatusCodes.Status401Unauthorized, problem.Status);
        Assert.Equal("Authentication failed", problem.Title);
        Assert.Equal("Invalid email or password.", problem.Detail);
    }

    [Fact]
    public async Task Refresh_InvalidToken_Returns401ProblemDetails()
    {
        var controller = new AuthController(new FakeAuthService
        {
            RefreshHandler = _ => throw new UnauthorizedAccessException("Invalid refresh token.")
        });

        var result = await controller.Refresh(new RefreshRequest("bad-token"));

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(unauthorized.Value);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
        Assert.Equal(StatusCodes.Status401Unauthorized, problem.Status);
        Assert.Equal("Refresh token rejected", problem.Title);
        Assert.Equal("Invalid refresh token.", problem.Detail);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409ProblemDetails()
    {
        var controller = new AuthController(new FakeAuthService
        {
            RegisterHandler = _ => throw new InvalidOperationException("Email is already registered.")
        });

        var result = await controller.Register(new RegisterRequest(
            "Test User",
            "test@example.com",
            "Password123!",
            "Dispatcher"));

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(conflict.Value);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
        Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
        Assert.Equal("Registration failed", problem.Title);
        Assert.Equal("Email is already registered.", problem.Detail);
    }

    [Fact]
    public async Task Revoke_InvalidToken_Returns400ProblemDetails()
    {
        var controller = new AuthController(new FakeAuthService
        {
            RevokeHandler = _ => throw new ArgumentException("Refresh token not found or already revoked.")
        });

        var result = await controller.Revoke(new RevokeRequest("bad-token"));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("Invalid revoke request", problem.Title);
        Assert.Equal("Refresh token not found or already revoked.", problem.Detail);
    }

    private sealed class FakeAuthService : IAuthService
    {
        public Func<RegisterRequest, AuthResponse> RegisterHandler { get; init; } =
            _ => new AuthResponse("access-token", "refresh-token", 3600);

        public Func<LoginRequest, AuthResponse> LoginHandler { get; init; } =
            _ => new AuthResponse("access-token", "refresh-token", 3600);

        public Func<RefreshRequest, AuthResponse> RefreshHandler { get; init; } =
            _ => new AuthResponse("access-token", "refresh-token", 3600);

        public Action<RevokeRequest> RevokeHandler { get; init; } = _ => { };

        public Task<AuthResponse> RegisterAsync(RegisterRequest request) =>
            Task.FromResult(RegisterHandler(request));

        public Task<AuthResponse> LoginAsync(LoginRequest request) =>
            Task.FromResult(LoginHandler(request));

        public Task<AuthResponse> RefreshAsync(RefreshRequest request) =>
            Task.FromResult(RefreshHandler(request));

        public Task RevokeAsync(RevokeRequest request)
        {
            RevokeHandler(request);
            return Task.CompletedTask;
        }
    }
}
