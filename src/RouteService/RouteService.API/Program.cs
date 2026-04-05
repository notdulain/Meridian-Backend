using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Meridian.VehicleGrpc;
using RouteService.API.Data;
using RouteService.API.Repositories;
using RouteService.API.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
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
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// Configure Redis distributed cache
var redisConfiguration = builder.Configuration.GetConnectionString("RedisCache");
if (string.IsNullOrWhiteSpace(redisConfiguration))
{
    throw new InvalidOperationException("ConnectionStrings:RedisCache is not configured.");
}

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConfiguration;
    options.InstanceName = "MeridianRoutes:";
});


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

// Configure gRPC Client
builder.Services.AddGrpcClient<VehicleGrpc.VehicleGrpcClient>(o =>
{
    o.Address = new Uri(builder.Configuration["Grpc:VehicleServiceUrl"]!);
});

// Configure HttpClient for Google Routes API (v2)
builder.Services.AddHttpClient(nameof(GoogleMapsService), client =>
{
    client.BaseAddress = new Uri("https://routes.googleapis.com");
    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Meridian-RouteService/1.0");
});
builder.Services.AddScoped<IGoogleMapsService, GoogleMapsService>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured.");

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

static async Task ApplyRouteMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<RouteServiceDbContext>();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("RouteHistoryMigrations");

    try
    {
        await dbContext.Database.MigrateAsync();
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
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to ensure RouteHistories fuel report schema changes.");
        throw;
    }
}
