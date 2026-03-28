using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using TrackingService.API.Models;

namespace TrackingService.API.Repositories;

public class TrackingRepository : ITrackingRepository
{
    private readonly string _connectionString;

    public TrackingRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("TrackingDb") 
            ?? throw new InvalidOperationException("TrackingDb connection string not found");
    }

    private IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public async Task EnsureDatabaseAsync()
    {
        var connBuilder = new SqlConnectionStringBuilder(_connectionString);
        var originalDb = connBuilder.InitialCatalog;
        
        connBuilder.InitialCatalog = "master";
        var masterConnectionStr = connBuilder.ConnectionString;

        using (var connection = new SqlConnection(masterConnectionStr))
        {
            var dbCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM sys.databases WHERE name = @DbName",
                new { DbName = originalDb });

            if (dbCount == 0)
            {
                await connection.ExecuteAsync($"CREATE DATABASE {originalDb}");
            }
        }

        using (var connection = CreateConnection())
        {
            var tableCreationSql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='LocationUpdates' and xtype='U')
                BEGIN
                    CREATE TABLE LocationUpdates (
                        LocationUpdateId INT IDENTITY(1,1) PRIMARY KEY,
                        AssignmentId INT NOT NULL,
                        DriverId INT NOT NULL,
                        Latitude DECIMAL(18,8) NOT NULL,
                        Longitude DECIMAL(18,8) NOT NULL,
                        Timestamp DATETIME2 NOT NULL,
                        SpeedKmh DECIMAL(10,2) NULL
                    );

                    CREATE INDEX IX_LocationUpdates_AssignmentId ON LocationUpdates(AssignmentId);
                    CREATE INDEX IX_LocationUpdates_DriverId ON LocationUpdates(DriverId);
                END";
            
            await connection.ExecuteAsync(tableCreationSql);
        }
    }

    public async Task<LocationUpdate> LogLocationAsync(LocationUpdate locationUpdate)
    {
        using var connection = CreateConnection();
        var sql = @"
            INSERT INTO LocationUpdates (AssignmentId, DriverId, Latitude, Longitude, Timestamp, SpeedKmh)
            OUTPUT INSERTED.LocationUpdateId
            VALUES (@AssignmentId, @DriverId, @Latitude, @Longitude, @Timestamp, @SpeedKmh);";

        var id = await connection.ExecuteScalarAsync<int>(sql, locationUpdate);
        locationUpdate.LocationUpdateId = id;
        return locationUpdate;
    }

    public async Task<IEnumerable<LocationUpdate>> GetHistoryAsync(int assignmentId)
    {
        using var connection = CreateConnection();
        return await connection.QueryAsync<LocationUpdate>(
            "SELECT * FROM LocationUpdates WHERE AssignmentId = @AssignmentId ORDER BY Timestamp ASC",
            new { AssignmentId = assignmentId });
    }

    public async Task<LocationUpdate?> GetLastKnownLocationAsync(int driverId)
    {
        using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<LocationUpdate>(
            @"SELECT TOP 1 * FROM LocationUpdates 
              WHERE DriverId = @DriverId 
              ORDER BY Timestamp DESC",
            new { DriverId = driverId });
    }
}
