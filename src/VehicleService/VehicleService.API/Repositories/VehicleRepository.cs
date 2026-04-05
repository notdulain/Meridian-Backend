using Microsoft.Data.SqlClient;
using VehicleService.API.Models;

namespace VehicleService.API.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly string _connectionString;
    private readonly string _deliveryConnectionString;
    private readonly string _routeConnectionString;

    public VehicleRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("VehicleDb") 
            ?? throw new InvalidOperationException("Connection string 'VehicleDb' not found.");

        var deliveryDatabaseName = configuration.GetValue<string>("Reporting:DeliveryDatabaseName") ?? "meridian_delivery";
        var routeDatabaseName = configuration.GetValue<string>("Reporting:RouteDatabaseName") ?? "meridian_route";

        _deliveryConnectionString = new SqlConnectionStringBuilder(_connectionString)
        {
            InitialCatalog = deliveryDatabaseName
        }.ConnectionString;

        _routeConnectionString = new SqlConnectionStringBuilder(_connectionString)
        {
            InitialCatalog = routeDatabaseName
        }.ConnectionString;
    }

    private SqlConnection GetConnection() => new(_connectionString);

    public async Task<Vehicle> CreateAsync(Vehicle vehicle)
    {
        using var connection = GetConnection();
        await connection.OpenAsync();

        var query = @"
            INSERT INTO Vehicles (PlateNumber, Make, Model, CurrentLocation, Year, CapacityKg, CapacityM3, FuelEfficiencyKmPerLitre, Status)
            OUTPUT INSERTED.VehicleId
            VALUES (@PlateNumber, @Make, @Model, @CurrentLocation, @Year, @CapacityKg, @CapacityM3, @FuelEfficiencyKmPerLitre, @Status);";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@PlateNumber", vehicle.PlateNumber);
        command.Parameters.AddWithValue("@Make", vehicle.Make);
        command.Parameters.AddWithValue("@Model", vehicle.Model);
        command.Parameters.AddWithValue("@CurrentLocation", vehicle.CurrentLocation);
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

    public async Task<(IEnumerable<Vehicle> Vehicles, int TotalCount)> GetAllAsync(int page, int pageSize, string? status, bool? isActive = null)
    {
        using var connection = GetConnection();
        await connection.OpenAsync();

        var filters = new List<string>();
        if (!string.IsNullOrEmpty(status)) filters.Add("Status = @Status");
        
        if (isActive.HasValue)
        {
            if (isActive.Value) filters.Add("Status != 'Retired'");
            else filters.Add("Status = 'Retired'");
        }
        
        var filterClause = filters.Count > 0 ? "WHERE " + string.Join(" AND ", filters) : "";
        var countQuery = $"SELECT COUNT(*) FROM Vehicles {filterClause}";
        
        using var countCommand = new SqlCommand(countQuery, connection);
        if (!string.IsNullOrEmpty(status)) countCommand.Parameters.AddWithValue("@Status", status);
        var totalCount = (int)await countCommand.ExecuteScalarAsync()!;

        var offset = (page - 1) * pageSize;
        var query = $"SELECT * FROM Vehicles {filterClause} ORDER BY VehicleId DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        
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

    public async Task<Vehicle?> GetByPlateNumberAsync(string plateNumber)
    {
        using var connection = GetConnection();
        await connection.OpenAsync();

        var query = "SELECT * FROM Vehicles WHERE PlateNumber = @PlateNumber";
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@PlateNumber", plateNumber);

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
            SET PlateNumber = @PlateNumber, Make = @Make, Model = @Model, CurrentLocation = @CurrentLocation, Year = @Year,
                CapacityKg = @CapacityKg, CapacityM3 = @CapacityM3, FuelEfficiencyKmPerLitre = @FuelEfficiencyKmPerLitre,
                Status = @Status, UpdatedAt = GETUTCDATE()
            WHERE VehicleId = @VehicleId";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@VehicleId", vehicle.VehicleId);
        command.Parameters.AddWithValue("@PlateNumber", vehicle.PlateNumber);
        command.Parameters.AddWithValue("@Make", vehicle.Make);
        command.Parameters.AddWithValue("@Model", vehicle.Model);
        command.Parameters.AddWithValue("@CurrentLocation", vehicle.CurrentLocation);
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

    public async Task<IEnumerable<VehicleUtilizationMetrics>> GetVehicleUtilizationReportAsync(DateTime? startDateUtc, DateTime? endDateUtc)
    {
        var windowStartUtc = startDateUtc ?? DateTime.UtcNow.AddDays(-30);
        var windowEndUtc = endDateUtc ?? DateTime.UtcNow;
        var reportWindowMinutes = Math.Max(0, (windowEndUtc - windowStartUtc).TotalMinutes);

        var activeVehicleIds = new List<int>();
        using (var vehicleConnection = GetConnection())
        {
            await vehicleConnection.OpenAsync();
            const string vehicleIdsQuery = "SELECT VehicleId FROM Vehicles WHERE Status <> 'Retired' ORDER BY VehicleId";
            using var vehicleCommand = new SqlCommand(vehicleIdsQuery, vehicleConnection);
            using var vehicleReader = await vehicleCommand.ExecuteReaderAsync();
            while (await vehicleReader.ReadAsync())
                activeVehicleIds.Add(vehicleReader.GetInt32(0));
        }

        var latestRouteDistanceByPath = new Dictionary<(string Origin, string Destination), (double DistanceKm, DateTime CreatedAt)>();
        using (var routeConnection = new SqlConnection(_routeConnectionString))
        {
            await routeConnection.OpenAsync();
            using var routeCommand = new SqlCommand(VehicleUtilizationReportQueryBuilder.BuildSelectedRouteHistoriesQuery(), routeConnection);
            using var routeReader = await routeCommand.ExecuteReaderAsync();
            while (await routeReader.ReadAsync())
            {
                var origin = routeReader.GetString(routeReader.GetOrdinal("Origin")).Trim().ToUpperInvariant();
                var destination = routeReader.GetString(routeReader.GetOrdinal("Destination")).Trim().ToUpperInvariant();
                var createdAt = routeReader.GetDateTime(routeReader.GetOrdinal("CreatedAt"));
                var key = (origin, destination);

                if (!latestRouteDistanceByPath.TryGetValue(key, out var existing) || createdAt > existing.CreatedAt)
                {
                    latestRouteDistanceByPath[key] = (
                        Convert.ToDouble(routeReader["DistanceKm"]),
                        createdAt
                    );
                }
            }
        }

        var metricsByVehicleId = new Dictionary<int, (int TripsCount, double KilometersDriven, double ActiveTripMinutes)>();
        using (var deliveryConnection = new SqlConnection(_deliveryConnectionString))
        {
            await deliveryConnection.OpenAsync();
            using var deliveryCommand = new SqlCommand(VehicleUtilizationReportQueryBuilder.BuildDeliveredTripsQuery(), deliveryConnection);
            deliveryCommand.Parameters.AddWithValue("@StartDateUtc", (object?)startDateUtc ?? DBNull.Value);
            deliveryCommand.Parameters.AddWithValue("@EndDateUtc", (object?)endDateUtc ?? DBNull.Value);

            using var deliveryReader = await deliveryCommand.ExecuteReaderAsync();
            while (await deliveryReader.ReadAsync())
            {
                var vehicleId = deliveryReader.GetInt32(deliveryReader.GetOrdinal("VehicleId"));
                var pickupAddress = deliveryReader.GetString(deliveryReader.GetOrdinal("PickupAddress")).Trim().ToUpperInvariant();
                var deliveryAddress = deliveryReader.GetString(deliveryReader.GetOrdinal("DeliveryAddress")).Trim().ToUpperInvariant();
                var tripMinutes = Convert.ToDouble(deliveryReader["TripMinutes"]);
                var routeKey = (pickupAddress, deliveryAddress);
                var distanceKm = latestRouteDistanceByPath.TryGetValue(routeKey, out var routeMatch)
                    ? routeMatch.DistanceKm
                    : 0;

                if (!metricsByVehicleId.TryGetValue(vehicleId, out var metric))
                    metric = (TripsCount: 0, KilometersDriven: 0d, ActiveTripMinutes: 0d);

                metricsByVehicleId[vehicleId] = (
                    metric.TripsCount + 1,
                    metric.KilometersDriven + distanceKm,
                    metric.ActiveTripMinutes + tripMinutes
                );
            }
        }

        return activeVehicleIds
            .Select(vehicleId =>
            {
                var metric = metricsByVehicleId.TryGetValue(vehicleId, out var value)
                    ? value
                    : (TripsCount: 0, KilometersDriven: 0d, ActiveTripMinutes: 0d);

                return new VehicleUtilizationMetrics
                {
                    VehicleId = vehicleId,
                    TripsCount = metric.TripsCount,
                    KilometersDriven = metric.KilometersDriven,
                    IdleTimeMinutes = Math.Max(0, reportWindowMinutes - metric.ActiveTripMinutes)
                };
            })
            .ToList();
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
            CurrentLocation = reader.GetString(reader.GetOrdinal("CurrentLocation")),
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
