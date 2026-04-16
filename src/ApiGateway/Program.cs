using ApiGateway.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

string ResolveRequiredGatewaySetting(string envName)
{
    var value = Environment.GetEnvironmentVariable(envName);
    if (!string.IsNullOrWhiteSpace(value))
    {
        return value;
    }

    throw new InvalidOperationException($"{envName} must be configured for {builder.Environment.EnvironmentName}.");
}

var ocelotFileName = builder.Environment.IsDevelopment()
    ? "ocelot.Development.json"
    : $"ocelot.{builder.Environment.EnvironmentName}.json";

if (!File.Exists(ocelotFileName))
{
    ocelotFileName = "ocelot.json";
}

var ocelotJsonText = File.ReadAllText(ocelotFileName);
string? diagnosticsDeliverySwaggerUrl;

if (builder.Environment.IsDevelopment())
{
    diagnosticsDeliverySwaggerUrl = "http://localhost:6001/swagger/index.html";
}
else
{
    var replacements = new Dictionary<string, string>
    {
        ["${DELIVERY_SERVICE_HOST}"] = ResolveRequiredGatewaySetting("DELIVERY_SERVICE_HOST"),
        ["${VEHICLE_SERVICE_HOST}"] = ResolveRequiredGatewaySetting("VEHICLE_SERVICE_HOST"),
        ["${DRIVER_SERVICE_HOST}"] = ResolveRequiredGatewaySetting("DRIVER_SERVICE_HOST"),
        ["${ASSIGNMENT_SERVICE_HOST}"] = ResolveRequiredGatewaySetting("ASSIGNMENT_SERVICE_HOST"),
        ["${ROUTE_SERVICE_HOST}"] = ResolveRequiredGatewaySetting("ROUTE_SERVICE_HOST"),
        ["${TRACKING_SERVICE_HOST}"] = ResolveRequiredGatewaySetting("TRACKING_SERVICE_HOST"),
        ["${USER_SERVICE_HOST}"] = ResolveRequiredGatewaySetting("USER_SERVICE_HOST"),
        ["${OCELOT_BASE_URL}"] = ResolveRequiredGatewaySetting("OCELOT_BASE_URL")
    };

    foreach (var replacement in replacements)
    {
        ocelotJsonText = ocelotJsonText.Replace(replacement.Key, replacement.Value);
    }

    diagnosticsDeliverySwaggerUrl = $"https://{replacements["${DELIVERY_SERVICE_HOST}"]}/swagger/index.html";
}

builder.Configuration.AddJsonStream(new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(ocelotJsonText)));

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

string ResolveServiceBaseUrl(string localBaseUrl, string envName)
{
    if (builder.Environment.IsDevelopment())
    {
        return localBaseUrl;
    }

    return $"https://{ResolveRequiredGatewaySetting(envName)}";
}

// ─────────────────────────────────────────────
// CORS – allow the React frontend with credentials
// ─────────────────────────────────────────────
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (allowedOrigins is null || allowedOrigins.Length == 0)
{
    allowedOrigins = ["http://localhost:3000"];
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
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

builder.Services.AddAuthorization();
builder.Services.AddControllers();
// ─────────────────────────────────────────────
// Application Insights – MER-323
// ─────────────────────────────────────────────
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IDashboardSummaryService, DashboardSummaryService>();
builder.Services.AddHttpClient("DeliveryService", client =>
{
    client.BaseAddress = new Uri(ResolveServiceBaseUrl("http://localhost:6001", "DELIVERY_SERVICE_HOST"));
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddHttpClient("VehicleService", client =>
{
    client.BaseAddress = new Uri(ResolveServiceBaseUrl("http://localhost:6002", "VEHICLE_SERVICE_HOST"));
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddHttpClient("DriverService", client =>
{
    client.BaseAddress = new Uri(ResolveServiceBaseUrl("http://localhost:6003", "DRIVER_SERVICE_HOST"));
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddHttpClient("AssignmentService", client =>
{
    client.BaseAddress = new Uri(ResolveServiceBaseUrl("http://localhost:6004", "ASSIGNMENT_SERVICE_HOST"));
    client.Timeout = TimeSpan.FromSeconds(15);
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
app.MapControllers();

app.UseWhen(
    context => context.Request.Path.Equals("/api/dashboard/summary", StringComparison.OrdinalIgnoreCase),
    dashboardApp =>
{
    dashboardApp.Run(async context =>
    {
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            return;
        }

        var authResult = await context.AuthenticateAsync("MeridianBearer");
        if (!authResult.Succeeded || authResult.Principal is null)
        {
            await context.ChallengeAsync("MeridianBearer");
            return;
        }

        context.User = authResult.Principal;

        if (!context.User.IsInRole("Admin") && !context.User.IsInRole("Dispatcher"))
        {
            await context.ForbidAsync("MeridianBearer");
            return;
        }

        var dashboardSummaryService = context.RequestServices.GetRequiredService<IDashboardSummaryService>();

        try
        {
            var summary = await dashboardSummaryService.GetSummaryAsync(context.RequestAborted);
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "application/json";
            
            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { success = true, data = summary }, jsonOptions));
        }
        catch (HttpRequestException ex)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "application/json";
            
            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                success = false,
                message = "Dashboard summary is temporarily unavailable.",
                errors = new[] { ex.Message }
            }, jsonOptions));
        }
    });
});

// Azure Container Apps health probe endpoint
app.MapGet("/", () => Results.Ok("Meridian API Gateway is running."));

app.MapGet("/diagnostics", async () => {
    try {
        var client = new System.Net.Http.HttpClient();
        client.Timeout = TimeSpan.FromSeconds(10);
        var response = await client.GetAsync(diagnosticsDeliverySwaggerUrl);
        var content = await response.Content.ReadAsStringAsync();
        return Results.Ok($"Connected to {diagnosticsDeliverySwaggerUrl}. Status: {response.StatusCode}. Content length: {content.Length}");
    } catch (Exception ex) {
        return Results.Problem(ex.ToString());
    }
});

await app.UseOcelot();

app.Run();
