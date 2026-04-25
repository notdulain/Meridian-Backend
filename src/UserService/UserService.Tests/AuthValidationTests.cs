// ============================================================
//  AuthValidationTests.cs
//  Feature : xUnit – IClassFixture + Mocking (Sumuditha)
//  Course  : SE3112 – Advanced Software Engineering
// ============================================================
//
//  KEY CONCEPTS (know these for your VIVA):
//
//  1. IClassFixture<T>
//     xUnit creates ONE instance of the fixture (UserServiceApplicationFactory)
//     and shares it across EVERY test in this class.
//     That means the in-memory web server starts only ONCE — saving time and
//     proving that all tests use the same isolated environment.
//
//  2. WebApplicationFactory<Program>
//     Microsoft's test helper that boots your real ASP.NET Core app in memory
//     without needing a real server or a real database.
//     Think of it as a "fake production environment" you control.
//
//  3. Mocking / Faking
//     We REPLACE the real IUserService (which would hit SQL Server) with a
//     FakeUserService.  That fake is registered into the DI container inside
//     ConfigureWebHost().  The real production code never even sees the swap —
//     it just gets whatever IUserService the container gives it.
//     This is the core of MOCKING: isolate what you are testing from things
//     you don't want to test (the database).
//
//  4. IAsyncLifetime
//     Gives the fixture an async InitializeAsync() (runs before any test) and
//     an async DisposeAsync() (runs after all tests finish).
//     Here we use it to log that the shared HttpClient was created once.
//
// ============================================================

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using UserService.API.DTOs;
using UserService.API.Services;
using Xunit;
using Xunit.Abstractions;

namespace UserService.Tests;

// ──────────────────────────────────────────────────────────────
//  FIXTURE — booted ONCE, shared by every test in the class
// ──────────────────────────────────────────────────────────────

/// <summary>
/// The FIXTURE.  WebApplicationFactory boots the real ASP.NET Core pipeline
/// in memory.  We override ConfigureWebHost() to:
///   (a) set the environment to "Testing" (skips DB migrations),
///   (b) inject known JWT settings so our helper can sign matching tokens, and
///   (c) REPLACE real services with fakes — this is the MOCKING step.
/// </summary>
public class UserServiceApplicationFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    // ── JWT constants used by both the server and the token helper ──
    private const string JwtSecret   = "super-secret-test-key-1234567890123456";
    private const string JwtIssuer   = "TestIssuer";
    private const string JwtAudience = "TestAudience";

    // Exposed so AuthValidationTests can build matching tokens
    public static readonly string TestJwtSecret   = JwtSecret;
    public static readonly string TestJwtIssuer   = JwtIssuer;
    public static readonly string TestJwtAudience = JwtAudience;

    // ── Wire-up the in-memory server ──
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Tell ASP.NET Core this is "Testing" — Program.cs skips DB migrations
        // and uses hard-coded JWT values (see Program.cs lines 82-102)
        builder.UseEnvironment("Testing");

        // Inject JWT config into the in-memory configuration system
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["Jwt:Secret"]   = JwtSecret,
                ["Jwt:Issuer"]   = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience
            };
            configBuilder.AddInMemoryCollection(settings!);
        });

        builder.ConfigureServices(services =>
        {
            // ── MOCKING ──
            // Remove whatever the real app registered for IUserService and
            // IDriverAccountProvisioningService (those need a live SQL DB).
            // Replace them with lightweight fakes that return canned data.
            // This is why our tests NEVER touch a database.
            services.RemoveAll<IUserService>();
            services.RemoveAll<IDriverAccountProvisioningService>();

            services.AddScoped<IUserService, FakeUserService>();
            services.AddScoped<IDriverAccountProvisioningService,
                               FakeDriverAccountProvisioningService>();
        });
    }

    // ── IAsyncLifetime — runs BEFORE the first test ──
    /// <summary>
    /// Called once before any test runs.  Useful for expensive async setup.
    /// Here we just confirm the factory is ready (in a real project you might
    /// seed a database or pre-warm a cache here).
    /// </summary>
    public Task InitializeAsync()
    {
        // Nothing expensive needed — the factory is already configured above.
        // Having this method shows xUnit's async lifecycle capability.
        return Task.CompletedTask;
    }

    // ── IAsyncLifetime — runs AFTER the last test ──
    /// <summary>
    /// Called once after all tests finish.  Dispose the server and free
    /// any resources.  base.Dispose() shuts down the in-memory web host.
    /// </summary>
    public new Task DisposeAsync()
    {
        Dispose(); // shuts down the in-memory ASP.NET Core server
        return Task.CompletedTask;
    }
}

// ──────────────────────────────────────────────────────────────
//  TEST CLASS — IClassFixture wires the shared fixture in
// ──────────────────────────────────────────────────────────────

/// <summary>
/// Integration-level auth tests.
///
/// IClassFixture&lt;UserServiceApplicationFactory&gt; tells xUnit:
///   "Create the factory ONCE, inject it into the constructor of every test
///    method in this class, then dispose it when the class is done."
///
/// All tests share one HttpClient, which talks to the same in-memory server.
/// </summary>
public class AuthValidationTests : IClassFixture<UserServiceApplicationFactory>
{
    private readonly HttpClient      _client;
    private readonly ITestOutputHelper _output;

    // xUnit injects the shared fixture and an optional output helper
    public AuthValidationTests(UserServiceApplicationFactory factory,
                               ITestOutputHelper output)
    {
        _client = factory.CreateClient();
        _output = output;
        _output.WriteLine("✔  Shared HttpClient obtained from fixture (server started once).");
    }

    // ══════════════════════════════════════════════════════════════
    //  GROUP 1 — Token Presence & Format
    //  Tests that the middleware correctly rejects bad or missing tokens
    //  BEFORE the controller even runs.
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// No Authorization header at all → 401 Unauthorized.
    /// The JWT middleware returns 401 before the controller is invoked.
    /// </summary>
    [Fact]
    public async Task MissingToken_Returns401()
    {
        _output.WriteLine("Test: calling /api/users/me with NO Authorization header.");

        var response = await _client.GetAsync("/api/users/me");

        _output.WriteLine($"Status received: {(int)response.StatusCode} {response.StatusCode}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// A random string that is not a JWT → 401.
    /// The middleware tries to parse the token and fails immediately.
    /// </summary>
    [Fact]
    public async Task InvalidToken_Returns401()
    {
        _output.WriteLine("Test: sending a completely random string as a Bearer token.");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "this-is-not-a-jwt");

        var response = await _client.SendAsync(request);

        _output.WriteLine($"Status received: {(int)response.StatusCode} {response.StatusCode}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// A real JWT with characters deleted from the middle → 401.
    /// Proves the middleware validates the cryptographic signature, not just
    /// the token format.
    /// </summary>
    [Fact]
    public async Task MalformedToken_SignatureCorrupted_Returns401()
    {
        _output.WriteLine("Test: take a valid JWT and corrupt its signature section.");

        var valid     = GenerateJwtToken(userId: 1, role: "Admin",
                                         expires: DateTime.UtcNow.AddMinutes(10));
        var corrupted = valid.Remove(valid.Length / 2, 5); // punch a hole in the middle

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", corrupted);

        var response = await _client.SendAsync(request);

        _output.WriteLine($"Status received: {(int)response.StatusCode} {response.StatusCode}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ══════════════════════════════════════════════════════════════
    //  GROUP 2 — Token Lifetime
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// A properly signed JWT that has already expired → 401.
    /// ClockSkew is set to Zero in Program.cs so even 1 second past expiry fails.
    /// </summary>
    [Fact]
    public async Task ExpiredToken_Returns401()
    {
        _output.WriteLine("Test: token expired 5 minutes ago — must be rejected.");

        var expiredToken = GenerateJwtToken(userId: 1, role: "Admin",
                                            expires: DateTime.UtcNow.AddMinutes(-5));

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await _client.SendAsync(request);

        _output.WriteLine($"Status received: {(int)response.StatusCode} {response.StatusCode}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ══════════════════════════════════════════════════════════════
    //  GROUP 3 — JWT Claim Validation (Issuer / Audience / Key)
    //  These are EDGE CASES that prove our server checks every field
    //  of the token, not just its format.
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Token signed with a different secret key → 401.
    /// Even if everything else is valid, a wrong signing key means the
    /// server cannot trust the token came from us.
    /// </summary>
    [Fact]
    public async Task WrongSigningKey_Returns401()
    {
        _output.WriteLine("Test: token signed with a completely different secret key.");

        var wrongKeyToken = GenerateJwtToken(
            userId:  1,
            role:    "Admin",
            expires: DateTime.UtcNow.AddMinutes(10),
            overrideSecret: "completely-different-key-987654321000"); // <-- wrong key

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", wrongKeyToken);

        var response = await _client.SendAsync(request);

        _output.WriteLine($"Status received: {(int)response.StatusCode} {response.StatusCode}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Token with a mismatched Issuer claim → 401.
    /// ValidateIssuer = true in Program.cs means the "iss" claim must match exactly.
    /// </summary>
    [Fact]
    public async Task WrongIssuer_Returns401()
    {
        _output.WriteLine("Test: valid token but 'iss' claim says 'EvilIssuer' not 'TestIssuer'.");

        var wrongIssuerToken = GenerateJwtToken(
            userId:         1,
            role:           "Admin",
            expires:        DateTime.UtcNow.AddMinutes(10),
            overrideIssuer: "EvilIssuer"); // <-- wrong issuer

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", wrongIssuerToken);

        var response = await _client.SendAsync(request);

        _output.WriteLine($"Status received: {(int)response.StatusCode} {response.StatusCode}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Token with a mismatched Audience claim → 401.
    /// ValidateAudience = true in Program.cs means the "aud" claim must match exactly.
    /// </summary>
    [Fact]
    public async Task WrongAudience_Returns401()
    {
        _output.WriteLine("Test: valid token but 'aud' claim says 'WrongApp' not 'TestAudience'.");

        var wrongAudToken = GenerateJwtToken(
            userId:           1,
            role:             "Admin",
            expires:          DateTime.UtcNow.AddMinutes(10),
            overrideAudience: "WrongApp"); // <-- wrong audience

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", wrongAudToken);

        var response = await _client.SendAsync(request);

        _output.WriteLine($"Status received: {(int)response.StatusCode} {response.StatusCode}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ══════════════════════════════════════════════════════════════
    //  GROUP 4 — Successful Authentication + Response Body Check
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// A perfectly valid Admin token → 200 OK.
    /// We also assert on the JSON response BODY — not just the status code —
    /// to prove the FakeUserService mock was invoked and returned real data.
    /// </summary>
    [Fact]
    public async Task ValidAdminToken_Returns200_WithUserBody()
    {
        _output.WriteLine("Test: valid Admin JWT — expect 200 and a populated user body.");

        var validToken = GenerateJwtToken(userId: 1, role: "Admin",
                                          expires: DateTime.UtcNow.AddMinutes(10));

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", validToken);

        var response = await _client.SendAsync(request);
        var body     = await response.Content.ReadAsStringAsync();

        _output.WriteLine($"Status: {(int)response.StatusCode}");
        _output.WriteLine($"Body:   {body}");

        // Status assertion
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Body assertions — proves the MOCK returned real-looking data
        var doc = JsonDocument.Parse(body).RootElement;
        Assert.Equal(1,              doc.GetProperty("userId").GetInt32());
        Assert.Equal("test@example.com", doc.GetProperty("email").GetString());
        Assert.True(doc.GetProperty("isActive").GetBoolean(),
                    "isActive should be true from FakeUserService");
    }

    /// <summary>
    /// A valid Dispatcher-role token can also reach /api/users/me → 200.
    /// The endpoint only requires [Authorize] (any role), not [Authorize(Roles="Admin")].
    /// </summary>
    [Fact]
    public async Task ValidDispatcherToken_Returns200()
    {
        _output.WriteLine("Test: Dispatcher role should be allowed on /api/users/me.");

        var dispatcherToken = GenerateJwtToken(userId: 5, role: "Dispatcher",
                                               expires: DateTime.UtcNow.AddMinutes(10));

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", dispatcherToken);

        var response = await _client.SendAsync(request);

        _output.WriteLine($"Status: {(int)response.StatusCode}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ══════════════════════════════════════════════════════════════
    //  GROUP 5 — Privilege Escalation (Authorization)
    //  These tests cross into Authorization — what a VALID user is
    //  ALLOWED to do.  Different from Authentication (who are you?).
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// HORIZONTAL privilege escalation:
    /// User ID 1 (role = "User") tries to read User ID 2's profile → 403.
    /// The controller checks: if you're not Admin AND the ID doesn't match your
    /// own token, return Forbid().
    /// </summary>
    [Fact]
    public async Task HorizontalPrivilegeEscalation_UserAccessingOtherUserData_Returns403()
    {
        _output.WriteLine("Test: User(id=1) tries GET /api/users/2 — should be blocked.");

        var token = GenerateJwtToken(userId: 1, role: "User",
                                     expires: DateTime.UtcNow.AddMinutes(10));

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/2");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        _output.WriteLine($"Status: {(int)response.StatusCode}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    /// VERTICAL privilege escalation:
    /// Standard "User" role calls GET /api/users (Admin-only endpoint) → 403.
    /// [Authorize(Roles = "Admin")] on the controller rejects non-Admin tokens.
    /// </summary>
    [Fact]
    public async Task VerticalPrivilegeEscalation_UserCallingAdminEndpoint_Returns403()
    {
        _output.WriteLine("Test: User role tries GET /api/users (Admin only) — should be blocked.");

        var token = GenerateJwtToken(userId: 1, role: "User",
                                     expires: DateTime.UtcNow.AddMinutes(10));

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        _output.WriteLine($"Status: {(int)response.StatusCode}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    /// Admin CAN access another user's profile (Admin bypass) → not 403.
    /// Verifies the positive path of the same controller logic.
    /// </summary>
    [Fact]
    public async Task AdminToken_AccessingAnyUserProfile_NotForbidden()
    {
        _output.WriteLine("Test: Admin(id=1) tries GET /api/users/2 — should NOT be blocked.");

        var adminToken = GenerateJwtToken(userId: 1, role: "Admin",
                                          expires: DateTime.UtcNow.AddMinutes(10));

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/2");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(request);

        _output.WriteLine($"Status: {(int)response.StatusCode}");
        // FakeUserService.GetByIdAsync returns null → 404, but NOT 403.
        // Any status code other than 403 proves the admin bypass works.
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ──────────────────────────────────────────────────────────────
    //  HELPER — builds a signed JWT for tests
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a signed JWT.  The optional override parameters let individual
    /// tests forge tokens with bad issuers, audiences, or keys to test rejection.
    /// </summary>
    private static string GenerateJwtToken(
        int       userId,
        string    role,
        DateTime? expires          = null,
        string?   overrideSecret   = null,
        string?   overrideIssuer   = null,
        string?   overrideAudience = null)
    {
        var secret   = overrideSecret   ?? UserServiceApplicationFactory.TestJwtSecret;
        var issuer   = overrideIssuer   ?? UserServiceApplicationFactory.TestJwtIssuer;
        var audience = overrideAudience ?? UserServiceApplicationFactory.TestJwtAudience;

        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "test@example.com"),
            new Claim(ClaimTypes.Role,               role),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer:            issuer,
            audience:          audience,
            claims:            claims,
            expires:           expires ?? DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// ──────────────────────────────────────────────────────────────
//  FAKES (test doubles / mocks)
// ──────────────────────────────────────────────────────────────

/// <summary>
/// MOCK for IUserService.
/// Registered into the DI container inside ConfigureWebHost() to replace the
/// real UserService that needs SQL Server.
///
/// VIVA TIP: "A mock/fake implements the same interface as the real service
/// but returns canned, predictable data so tests don't depend on a database."
/// </summary>
internal class FakeUserService : IUserService
{
    // GetAll returns an empty list — the Admin-only endpoint will return 404
    public Task<IEnumerable<UserResponse>> GetAllAsync() =>
        Task.FromResult<IEnumerable<UserResponse>>(Array.Empty<UserResponse>());

    // GetById returns null — controller returns 404 (used for Admin bypass test)
    public Task<UserResponse?> GetByIdAsync(int userId) =>
        Task.FromResult<UserResponse?>(null);

    // GetMe returns a fixed user — controller returns 200 with this data
    public Task<UserResponse?> GetMeAsync(int userId)
    {
        var user = new UserResponse(
            UserId:    userId,
            FullName:  "Test User",
            Email:     "test@example.com",
            Role:      "Admin",
            IsActive:  true,
            CreatedAt: DateTime.UtcNow.AddDays(-1),
            UpdatedAt: DateTime.UtcNow);

        return Task.FromResult<UserResponse?>(user);
    }

    public Task<UserResponse?> UpdateAsync(int userId, UpdateUserRequest request) =>
        Task.FromResult<UserResponse?>(null);

    public Task<bool> SoftDeleteAsync(int userId) =>
        Task.FromResult(false);
}

/// <summary>
/// MOCK for IDriverAccountProvisioningService.
/// Replaced so the controller can be instantiated without needing a real
/// HTTP client pointed at the Driver microservice.
/// </summary>
internal sealed class FakeDriverAccountProvisioningService : IDriverAccountProvisioningService
{
    public Task<CreateDriverAccountResponse> CreateDriverAccountAsync(
        CreateDriverAccountRequest request,
        string                     authorizationHeader,
        CancellationToken          cancellationToken = default)
    {
        var response = new CreateDriverAccountResponse(
            new UserResponse(
                UserId:    99,
                FullName:  request.FullName,
                Email:     request.Email,
                Role:      "Driver",
                IsActive:  true,
                CreatedAt: DateTime.UtcNow,
                UpdatedAt: DateTime.UtcNow),
            new DriverProfileResponse(
                DriverId:                 99,
                UserId:                   "99",
                FullName:                 request.FullName,
                LicenseNumber:            request.LicenseNumber,
                LicenseExpiry:            request.LicenseExpiry,
                PhoneNumber:              request.PhoneNumber,
                MaxWorkingHoursPerDay:    request.MaxWorkingHoursPerDay,
                CurrentWorkingHoursToday: 0,
                IsActive:                 true,
                CreatedAt:                DateTime.UtcNow,
                UpdatedAt:                DateTime.UtcNow));

        return Task.FromResult(response);
    }
}
