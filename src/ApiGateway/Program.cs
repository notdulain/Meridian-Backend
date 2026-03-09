using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

string ResolveGatewaySetting(string envName, string developmentFallback)
{
    var value = Environment.GetEnvironmentVariable(envName);
    if (!string.IsNullOrWhiteSpace(value))
    {
        return value;
    }

    if (builder.Environment.IsDevelopment())
    {
        return developmentFallback;
    }

    throw new InvalidOperationException($"{envName} must be configured outside Development.");
}

// ─────────────────────────────────────────────
// Configuration (Dynamic Ocelot Environment Variables)
// ─────────────────────────────────────────────
var deliveryServiceHost = ResolveGatewaySetting("DELIVERY_SERVICE_HOST", "ca-delivery-service");
var vehicleServiceHost = ResolveGatewaySetting("VEHICLE_SERVICE_HOST", "ca-vehicle-service");
var driverServiceHost = ResolveGatewaySetting("DRIVER_SERVICE_HOST", "ca-driver-service");
var assignmentServiceHost = ResolveGatewaySetting("ASSIGNMENT_SERVICE_HOST", "ca-assignment-service");
var routeServiceHost = ResolveGatewaySetting("ROUTE_SERVICE_HOST", "ca-route-service");
var trackingServiceHost = ResolveGatewaySetting("TRACKING_SERVICE_HOST", "ca-tracking-service");
var userServiceHost = ResolveGatewaySetting("USER_SERVICE_HOST", "ca-user-service");
var ocelotBaseUrl = ResolveGatewaySetting("OCELOT_BASE_URL", "http://localhost:5050");

var ocelotJsonText = System.IO.File.ReadAllText("ocelot.json");
ocelotJsonText = ocelotJsonText.Replace("${DELIVERY_SERVICE_HOST}", deliveryServiceHost);
ocelotJsonText = ocelotJsonText.Replace("${VEHICLE_SERVICE_HOST}", vehicleServiceHost);
ocelotJsonText = ocelotJsonText.Replace("${DRIVER_SERVICE_HOST}", driverServiceHost);
ocelotJsonText = ocelotJsonText.Replace("${ASSIGNMENT_SERVICE_HOST}", assignmentServiceHost);
ocelotJsonText = ocelotJsonText.Replace("${ROUTE_SERVICE_HOST}", routeServiceHost);
ocelotJsonText = ocelotJsonText.Replace("${TRACKING_SERVICE_HOST}", trackingServiceHost);
ocelotJsonText = ocelotJsonText.Replace("${USER_SERVICE_HOST}", userServiceHost);
ocelotJsonText = ocelotJsonText.Replace("${OCELOT_BASE_URL}", ocelotBaseUrl);

builder.Configuration.AddJsonStream(new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(ocelotJsonText)));

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

// Azure Container Apps health probe endpoint
app.MapGet("/", () => Results.Ok("Meridian API Gateway is running."));

app.MapGet("/diagnostics", async () => {
    try {
        var client = new System.Net.Http.HttpClient();
        client.Timeout = TimeSpan.FromSeconds(10);
        var url = $"https://{deliveryServiceHost}/swagger/index.html";
        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        return Results.Ok($"Connected to {url}. Status: {response.StatusCode}. Content length: {content.Length}");
    } catch (Exception ex) {
        return Results.Problem(ex.ToString());
    }
});

await app.UseOcelot();

app.Run();
