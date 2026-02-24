using Microsoft.Data.SqlClient;

namespace VehicleService.API.Data;

public class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("VehicleDb") 
            ?? throw new InvalidOperationException("Connection string 'VehicleDb' not found.");
    }

    public void Initialize()
    {
        var builder = new SqlConnectionStringBuilder(_connectionString);
        var databaseName = builder.InitialCatalog;
        builder.InitialCatalog = "master"; 
        var masterConnection = builder.ConnectionString;

        using (var connection = new SqlConnection(masterConnection))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $@"
                    IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '{databaseName}')
                    BEGIN
                        CREATE DATABASE [{databaseName}];
                    END";
                command.ExecuteNonQuery();
            }
        }

        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Vehicles' AND xtype='U')
                    BEGIN
                        CREATE TABLE Vehicles (
                            VehicleId INT IDENTITY(1,1) PRIMARY KEY,
                            PlateNumber NVARCHAR(50) NOT NULL UNIQUE,
                            Make NVARCHAR(100) NOT NULL,
                            Model NVARCHAR(100) NOT NULL,
                            Year INT NOT NULL,
                            CapacityKg FLOAT NOT NULL,
                            CapacityM3 FLOAT NOT NULL,
                            FuelEfficiencyKmPerLitre FLOAT NOT NULL,
                            Status NVARCHAR(50) NOT NULL DEFAULT 'Available',
                            CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                            UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                        );
                    END";
                command.ExecuteNonQuery();
            }
        }
    }
}
