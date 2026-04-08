using DbUp;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Text;
using DriverService.API.Grpc;
using DriverService.API.Repositories;
using DriverService.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel((context, options) =>
{
    if (builder.Environment.IsDevelopment())
    {
        var restPort = context.Configuration.GetValue<int?>("Ports:DriverServiceHttp") ?? 6003;
        var grpcPort = context.Configuration.GetValue<int?>("Ports:DriverServiceGrpc") ?? 7003;

        options.ListenLocalhost(restPort, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http1;
        });

        options.ListenLocalhost(grpcPort, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http2;
        });

        return;
    }

    var serviceMode = context.Configuration["ServiceMode"];

    // gRPC-only container apps use h2c on the ACA target port.
    if (string.Equals(serviceMode, "GrpcOnly", StringComparison.OrdinalIgnoreCase))
    {
        options.ListenAnyIP(8080, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http2;
        });

        return;
    }

    // REST container apps use HTTP/1.1 on the ACA target port.
    options.ListenAnyIP(8080, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1;
    });
});

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "DriverService API", Version = "v1" });
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

builder.Services.AddGrpc();

// DI Configurations
builder.Services.AddScoped<IDriverRepository, DriverRepository>();
builder.Services.AddScoped<IDriverService, DriverService.API.Services.DriverService>();

// JWT Authentication Configuration
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Run DbUp Migrations
var connectionString = builder.Configuration.GetConnectionString("DriverDb");
EnsureDatabase.For.SqlDatabase(connectionString);

var upgrader = DeployChanges.To
    .SqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
    .LogToConsole()
    .Build();

var result = upgrader.PerformUpgrade();
if (!result.Successful)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(result.Error);
    Console.ResetColor();
    throw new Exception("Database migration failed", result.Error);
}
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Database migration successful!");
Console.ResetColor();

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
        c.SwaggerEndpoint("v1/swagger.json", "DriverService v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGrpcService<DriverGrpcService>();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
