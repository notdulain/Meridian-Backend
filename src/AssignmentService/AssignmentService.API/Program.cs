using Microsoft.AspNetCore.Authentication.JwtBearer;
using Serilog;
using Meridian.VehicleGrpc;
using Meridian.DriverGrpc;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

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
    client.BaseAddress = new Uri("http://localhost:6001");
});

// Keycloak Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience = builder.Configuration["Keycloak:Audience"];
        options.RequireHttpsMetadata = false; // dev only
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapGet("/swagger", () => Results.Content(
        """
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1" />
          <title>Swagger UI</title>
          <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/swagger-ui-dist@5/swagger-ui.css" />
          <style>body { margin: 0; } #swagger-ui { max-width: 100%; }</style>
        </head>
        <body>
          <div id="swagger-ui"></div>
          <script src="https://cdn.jsdelivr.net/npm/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
          <script>
            window.ui = SwaggerUIBundle({
              url: '/openapi/v1.json',
              dom_id: '#swagger-ui'
            });
          </script>
        </body>
        </html>
        """,
        "text/html"));
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
