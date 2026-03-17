using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Meridian.VehicleGrpc;
using Meridian.DriverGrpc;
using System.Text;
using Microsoft.Data.SqlClient;
using Dapper;
using AssignmentService.API.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AssignmentService API", Version = "v1" });
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

// Configure gRPC Clients
builder.Services.AddGrpcClient<VehicleGrpc.VehicleGrpcClient>(o =>
{
    o.Address = new Uri(builder.Configuration["Grpc:VehicleServiceUrl"]!);
});

builder.Services.AddGrpcClient<DriverGrpc.DriverGrpcClient>(o =>
{
    o.Address = new Uri(builder.Configuration["Grpc:DriverServiceUrl"]!);
});

// Configure HttpClient for DeliveryService
builder.Services.AddHttpClient("DeliveryService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:DeliveryServiceUrl"]!);
});

// Register repository
builder.Services.AddScoped<IAssignmentRepository, AssignmentRepository>();

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

// Run DB migration
var connectionString = builder.Configuration.GetConnectionString("AssignmentDb");
if (!string.IsNullOrEmpty(connectionString))
{
    try
    {
        // 1. Ensure database exists
        var masterConnectionString = connectionString.Replace("Database=assignment_db;", "Database=master;");
        using (var masterConn = new SqlConnection(masterConnectionString))
        {
            await masterConn.ExecuteAsync("IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'assignment_db') CREATE DATABASE assignment_db;");
        }

        // 2. Run table migration
        using var conn = new SqlConnection(connectionString);
        var sql = @"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Assignments' AND xtype='U')
            BEGIN
                CREATE TABLE Assignments (
                    AssignmentId INT IDENTITY(1,1) PRIMARY KEY,
                    DeliveryId INT NOT NULL,
                    VehicleId INT NOT NULL,
                    DriverId INT NOT NULL,
                    AssignedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
                    AssignedBy NVARCHAR(255) NOT NULL,
                    Status NVARCHAR(50) NOT NULL DEFAULT 'Active',
                    Notes NVARCHAR(MAX) NULL,
                    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
                    UpdatedAt DATETIME NOT NULL DEFAULT GETUTCDATE()
                )
            END";
        await conn.ExecuteAsync(sql);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Database migration successful!");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Database migration failed: {ex.Message}");
        Console.ResetColor();
    }
}

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
        c.SwaggerEndpoint("v1/swagger.json", "AssignmentService v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
