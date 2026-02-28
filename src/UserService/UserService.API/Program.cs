using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using UserService.API.Data;
using UserService.API.Repositories;
using UserService.API.Services;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────
// Serilog
// ─────────────────────────────────────────────
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console());

// ─────────────────────────────────────────────
// Controllers
// ─────────────────────────────────────────────
builder.Services.AddControllers();

// ─────────────────────────────────────────────
// HttpContextAccessor
// ─────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();

// ─────────────────────────────────────────────
// JWT Authentication — symmetric signing key
// ─────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("MeridianBearer", options =>
    {
        options.RequireHttpsMetadata = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// ─────────────────────────────────────────────
// Authorization
// ─────────────────────────────────────────────
builder.Services.AddAuthorization();

// ─────────────────────────────────────────────
// CORS
// ─────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ─────────────────────────────────────────────
// Repositories & Services (scoped)
// ─────────────────────────────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService.API.Services.UserService>();

// ─────────────────────────────────────────────
// DatabaseInitializer
// ─────────────────────────────────────────────
builder.Services.AddSingleton<DatabaseInitializer>();

// ─────────────────────────────────────────────
// Build & configure pipeline
// ─────────────────────────────────────────────
var app = builder.Build();

// Run database initialization before starting
var dbInit = app.Services.GetRequiredService<DatabaseInitializer>();
await dbInit.InitializeAsync();

app.UseCors("ReactFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
