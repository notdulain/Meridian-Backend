using Microsoft.Data.SqlClient;
using VehicleService.API.Models;

namespace VehicleService.API.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly string _connectionString;

    public VehicleRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("VehicleDb") 
            ?? throw new InvalidOperationException("Connection string 'VehicleDb' not found.");
    }

    private SqlConnection GetConnection() => new(_connectionString);

    public async Task<Vehicle> CreateAsync(Vehicle vehicle)
    {
        using var connection = GetConnection();
        await connection.OpenAsync();

        var query = @"
            INSERT INTO Vehicles (PlateNumber, Make, Model, Year, CapacityKg, CapacityM3, FuelEfficiencyKmPerLitre, Status)
            OUTPUT INSERTED.VehicleId
            VALUES (@PlateNumber, @Make, @Model, @Year, @CapacityKg, @CapacityM3, @FuelEfficiencyKmPerLitre, @Status);";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@PlateNumber", vehicle.PlateNumber);
        command.Parameters.AddWithValue("@Make", vehicle.Make);
        command.Parameters.AddWithValue("@Model", vehicle.Model);
        command.Parameters.AddWithValue("@Year", vehicle.Year);
        command.Parameters.AddWithValue("@CapacityKg", vehicle.CapacityKg);
        command.Parameters.AddWithValue("@CapacityM3", vehicle.CapacityM3);
        command.Parameters.AddWithValue("@FuelEfficiencyKmPerLitre", vehicle.FuelEfficiencyKmPerLitre);
        command.Parameters.AddWithValue("@Status", string.IsNullOrEmpty(vehicle.Status) ? "Available" : vehicle.Status);

        vehicle.VehicleId = (int)await command.ExecuteScalarAsync()!;
        
        // Fetch created/updated at
        var created = await GetByIdAsync(vehicle.VehicleId);
        if (created != null)
        {
            vehicle.CreatedAt = created.CreatedAt;
            vehicle.UpdatedAt = created.UpdatedAt;
        }

        return vehicle;
    }

    public async Task<(IEnumerable<Vehicle> Vehicles, int TotalCount)> GetAllAsync(int page, int pageSize, string? status)
    {
        using var connection = GetConnection();
        await connection.OpenAsync();

        var statusFilter = string.IsNullOrEmpty(status) ? "" : "WHERE Status = @Status";
        var countQuery = $"SELECT COUNT(*) FROM Vehicles {statusFilter}";
        
        using var countCommand = new SqlCommand(countQuery, connection);
        if (!string.IsNullOrEmpty(status)) countCommand.Parameters.AddWithValue("@Status", status);
        var totalCount = (int)await countCommand.ExecuteScalarAsync()!;

        var offset = (page - 1) * pageSize;
        var query = $"SELECT * FROM Vehicles {statusFilter} ORDER BY VehicleId DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        
        using var command = new SqlCommand(query, connection);
        if (!string.IsNullOrEmpty(status)) command.Parameters.AddWithValue("@Status", status);
        command.Parameters.AddWithValue("@PageSize", pageSize);
        command.Parameters.AddWithValue("@Offset", offset);

        var vehicles = new List<Vehicle>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            vehicles.Add(MapToVehicle(reader));
        }

        return (vehicles, totalCount);
    }

    public async Task<Vehicle?> GetByIdAsync(int id)
    {
        using var connection = GetConnection();
        await connection.OpenAsync();

        var query = "SELECT * FROM Vehicles WHERE VehicleId = @Id";
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@Id", id);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapToVehicle(reader);
        }

        return null;
    }

    public async Task<Vehicle> UpdateAsync(Vehicle vehicle)
    {
        using var connection = GetConnection();
        await connection.OpenAsync();

        var query = @"
            UPDATE Vehicles
            SET PlateNumber = @PlateNumber, Make = @Make, Model = @Model, Year = @Year,
                CapacityKg = @CapacityKg, CapacityM3 = @CapacityM3, FuelEfficiencyKmPerLitre = @FuelEfficiencyKmPerLitre,
                Status = @Status, UpdatedAt = GETUTCDATE()
            WHERE VehicleId = @VehicleId";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@VehicleId", vehicle.VehicleId);
        command.Parameters.AddWithValue("@PlateNumber", vehicle.PlateNumber);
        command.Parameters.AddWithValue("@Make", vehicle.Make);
        command.Parameters.AddWithValue("@Model", vehicle.Model);
        command.Parameters.AddWithValue("@Year", vehicle.Year);
        command.Parameters.AddWithValue("@CapacityKg", vehicle.CapacityKg);
        command.Parameters.AddWithValue("@CapacityM3", vehicle.CapacityM3);
        command.Parameters.AddWithValue("@FuelEfficiencyKmPerLitre", vehicle.FuelEfficiencyKmPerLitre);
        command.Parameters.AddWithValue("@Status", vehicle.Status);

        await command.ExecuteNonQueryAsync();

        // Refresh timestamps
        var updated = await GetByIdAsync(vehicle.VehicleId);
        if (updated != null)
        {
            vehicle.UpdatedAt = updated.UpdatedAt;
        }

        return vehicle;
    }

    public async Task<bool> UpdateStatusAsync(int id, string status)
    {
        using var connection = GetConnection();
        await connection.OpenAsync();

        var query = "UPDATE Vehicles SET Status = @Status, UpdatedAt = GETUTCDATE() WHERE VehicleId = @Id";
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@Status", status);
        command.Parameters.AddWithValue("@Id", id);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        // Soft delete -> Status = Retired
        return await UpdateStatusAsync(id, "Retired");
    }

    public async Task<IEnumerable<Vehicle>> GetAvailableAsync()
    {
        using var connection = GetConnection();
        await connection.OpenAsync();

        var query = "SELECT * FROM Vehicles WHERE Status = 'Available'";
        using var command = new SqlCommand(query, connection);

        var vehicles = new List<Vehicle>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            vehicles.Add(MapToVehicle(reader));
        }

        return vehicles;
    }

    private static Vehicle MapToVehicle(System.Data.Common.DbDataReader dbReader)
    {
        var reader = (SqlDataReader)dbReader;
        return new Vehicle
        {
            VehicleId = reader.GetInt32(reader.GetOrdinal("VehicleId")),
            PlateNumber = reader.GetString(reader.GetOrdinal("PlateNumber")),
            Make = reader.GetString(reader.GetOrdinal("Make")),
            Model = reader.GetString(reader.GetOrdinal("Model")),
            Year = reader.GetInt32(reader.GetOrdinal("Year")),
            CapacityKg = reader.GetDouble(reader.GetOrdinal("CapacityKg")),
            CapacityM3 = reader.GetDouble(reader.GetOrdinal("CapacityM3")),
            FuelEfficiencyKmPerLitre = reader.GetDouble(reader.GetOrdinal("FuelEfficiencyKmPerLitre")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
        };
    }
}
