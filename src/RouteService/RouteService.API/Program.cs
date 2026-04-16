using System.Text;
using Meridian.VehicleGrpc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RouteService.API.Data;
using RouteService.API.Repositories;
using RouteService.API.Services;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// ─────────────────────────────────────────────
// Application Insights – MER-323
// ─────────────────────────────────────────────
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "RouteService API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT access token (without the 'Bearer ' prefix)"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Configure distributed cache
ConfigureDistributedCache(builder);

var routeDbConnectionString = builder.Configuration.GetConnectionString("RouteDb");
if (string.IsNullOrWhiteSpace(routeDbConnectionString))
{
    throw new InvalidOperationException("ConnectionStrings:RouteDb is not configured.");
}

builder.Services.AddDbContext<RouteServiceDbContext>(options =>
    options.UseSqlServer(routeDbConnectionString));

builder.Services.AddScoped<IRouteHistoryRepository, RouteHistoryRepository>();
builder.Services.AddScoped<IRouteDecisionService, RouteDecisionService>();
builder.Services.AddScoped<IFuelCostReportService, FuelCostReportService>();

// Configure gRPC client
builder.Services.AddGrpcClient<VehicleGrpc.VehicleGrpcClient>(options =>
{
    options.Address = new Uri(builder.Configuration["Grpc:VehicleServiceUrl"]!);
});

// Configure HttpClient for Google Routes API
builder.Services.AddHttpClient(nameof(GoogleMapsService), client =>
{
    client.BaseAddress = new Uri("https://routes.googleapis.com");
    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Meridian-RouteService/1.0");
});
builder.Services.AddScoped<IGoogleMapsService, GoogleMapsService>();

// JWT authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

await ApplyRouteMigrationsAsync(app);
await EnsureRouteHistoryFuelReportColumnsAsync(app);

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger(c =>
    {
        c.PreSerializeFilters.Add((swagger, _) =>
        {
            var serverBasePath = app.Configuration["Swagger:ServerBasePath"];
            if (!string.IsNullOrWhiteSpace(serverBasePath))
            {
                swagger.Servers = new List<OpenApiServer> { new() { Url = serverBasePath } };
            }
        });
    });

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("v1/swagger.json", "RouteService v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static void ConfigureDistributedCache(WebApplicationBuilder builder)
{
    string? redisConnectionString =
        builder.Configuration["Redis:ConnectionString"] ??
        builder.Configuration.GetConnectionString("RedisCache");

    if (string.IsNullOrWhiteSpace(redisConnectionString))
    {
        Log.Warning("Redis cache is not configured. RouteService will continue with in-memory distributed cache.");
        builder.Services.AddDistributedMemoryCache();
        return;
    }

    try
    {
        StackExchange.Redis.ConfigurationOptions redisOptions =
            StackExchange.Redis.ConfigurationOptions.Parse(redisConnectionString);
        redisOptions.AbortOnConnectFail = false;
        redisOptions.ConnectRetry = 3;
        redisOptions.ConnectTimeout = 5000;
        redisOptions.AsyncTimeout = 5000;

        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.ConfigurationOptions = redisOptions;
            options.InstanceName = "MeridianRoutes:";
        });
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Redis cache configuration is invalid. RouteService will continue with in-memory distributed cache.");
        builder.Services.AddDistributedMemoryCache();
    }
}

static async Task ApplyRouteMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<RouteServiceDbContext>();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("RouteHistoryMigrations");

    try
    {
        logger.LogInformation("Applying RouteService migrations.");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("RouteService migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to apply RouteService migrations.");
        throw;
    }
}

static async Task EnsureRouteHistoryFuelReportColumnsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<RouteServiceDbContext>();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("RouteHistorySchemaGuard");

    const string sql =
        """
        IF OBJECT_ID(N'[dbo].[RouteHistories]', N'U') IS NOT NULL
        BEGIN
            IF COL_LENGTH(N'[dbo].[RouteHistories]', N'VehicleId') IS NULL
            BEGIN
                ALTER TABLE [dbo].[RouteHistories] ADD [VehicleId] int NULL;
            END

            IF COL_LENGTH(N'[dbo].[RouteHistories]', N'DriverId') IS NULL
            BEGIN
                ALTER TABLE [dbo].[RouteHistories] ADD [DriverId] int NULL;
            END

            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = N'IX_RouteHistories_VehicleId_DriverId_CreatedAt'
                  AND object_id = OBJECT_ID(N'[dbo].[RouteHistories]')
            )
            BEGIN
                CREATE INDEX [IX_RouteHistories_VehicleId_DriverId_CreatedAt]
                ON [dbo].[RouteHistories]([VehicleId], [DriverId], [CreatedAt]);
            END
        END
        """;

    try
    {
        await dbContext.Database.ExecuteSqlRawAsync(sql);
        logger.LogInformation("RouteHistories fuel report schema guard completed.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to ensure RouteHistories fuel report schema changes.");
        throw;
    }
}
