using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────
// Configuration
// ─────────────────────────────────────────────
builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

// ─────────────────────────────────────────────
// CORS – allow the React frontend with credentials
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
// JWT Bearer – symmetric signing key
// ─────────────────────────────────────────────
builder.Services
    .AddAuthentication()
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
// Ocelot
// ─────────────────────────────────────────────
builder.Services.AddOcelot();

// ─────────────────────────────────────────────
// Build & configure pipeline
// ─────────────────────────────────────────────
var app = builder.Build();

app.UseCors("ReactFrontend");

app.UseAuthentication();
app.UseAuthorization();

await app.UseOcelot();

app.Run();
