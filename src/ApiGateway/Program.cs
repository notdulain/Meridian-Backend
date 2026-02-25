using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────
// Configuration
// ─────────────────────────────────────────────
builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

var wso2Authority = builder.Configuration["WSO2:Authority"]
    ?? throw new InvalidOperationException("WSO2:Authority is not configured.");
var wso2Audience = builder.Configuration["WSO2:Audience"]
    ?? throw new InvalidOperationException("WSO2:Audience is not configured.");

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
// JWT Bearer – WSO2 Identity Server
//
// RequireHttpsMetadata = false   → allow http upstream calls in dev
// BackchannelHttpHandler         → accepts WSO2's self-signed TLS cert
// options.Configuration          → supplies the JwksUri directly so the
//                                   middleware fetches signing keys from the
//                                   JWKS endpoint without needing OIDC discovery
// ─────────────────────────────────────────────
builder.Services
    .AddAuthentication()
    .AddJwtBearer("WSO2Bearer", options =>
    {
        options.Authority = wso2Authority;
        options.RequireHttpsMetadata = false;

        // Accept WSO2's self-signed certificate during local development
        options.BackchannelHttpHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        // Manually set JwksUri inside an OpenIdConnectConfiguration so the
        // JwtBearer middleware can resolve signing keys without relying on a
        // successful OIDC discovery document (WSO2 IS uses a non-standard
        // discovery URL that may differ from the authority).
        options.Configuration = new OpenIdConnectConfiguration
        {
            JwksUri = "https://localhost:9443/oauth2/jwks"
        };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,          // WSO2 token issuer varies by tenant
            ValidateAudience = true,
            ValidAudience = wso2Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
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
