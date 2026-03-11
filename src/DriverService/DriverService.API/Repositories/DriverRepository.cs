using Microsoft.Data.SqlClient;
using DriverService.API.Models;

namespace DriverService.API.Repositories;

public class DriverRepository : IDriverRepository
{
    private readonly string _connectionString;

    public DriverRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DriverDb")
            ?? throw new InvalidOperationException("DriverDb connection string is not configured.");
    }

    private SqlConnection GetConnection() => new SqlConnection(_connectionString);

    public async Task<Driver> CreateAsync(Driver driver)
    {
        using var connection = GetConnection();
        await connection.OpenAsync();

        var query = @"
            INSERT INTO Drivers (UserId, FullName, LicenseNumber, LicenseExpiry, PhoneNumber, MaxWorkingHoursPerDay, IsActive, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.DriverId
            VALUES (@UserId, @FullName, @LicenseNumber, @LicenseExpiry, @PhoneNumber, @MaxWorkingHoursPerDay, 1, GETUTCDATE(), GETUTCDATE())";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@UserId", driver.UserId);
        command.Parameters.AddWithValue("@FullName", driver.FullName);
        command.Parameters.AddWithValue("@LicenseNumber", driver.LicenseNumber);
        command.Parameters.AddWithValue("@LicenseExpiry", driver.LicenseExpiry);
        command.Parameters.AddWithValue("@PhoneNumber", driver.PhoneNumber);
        command.Parameters.AddWithValue("@MaxWorkingHoursPerDay", driver.MaxWorkingHoursPerDay > 0 ? driver.MaxWorkingHoursPerDay : 8.0);

        driver.DriverId = (int)await command.ExecuteScalarAsync()!;
        return driver;
    }

    public async Task<(IEnumerable<Driver> Drivers, int TotalCount)> GetAllAsync(int page, int pageSize)
    {
        using var connection = GetConnection();
        await connection.OpenAsync();

        var countQuery = "SELECT COUNT(*) FROM Drivers WHERE IsActive = 1";
        using var countCommand = new SqlCommand(countQuery, connection);
        var totalCount = (int)await countCommand.ExecuteScalarAsync()!;

        var offset = (page - 1) * pageSize;
        var query = "SELECT * FROM Drivers WHERE IsActive = 1 ORDER BY DriverId DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@Offset", offset);
        command.Parameters.AddWithValue("@PageSize", pageSize);

        var drivers = new List<Driver>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            drivers.Add(MapToDriver(reader));

        return (drivers, totalCount);
    }

    public async Task<(IEnumerable<Driver> Drivers, int TotalCount)> GetDeletedAsync(int page, int pageSize)
    {
        using var connection = GetConnection();
        await connection.OpenAsync();

        var countQuery = "SELECT COUNT(*) FROM Drivers WHERE IsActive = 0";
        using var countCommand = new SqlCommand(countQuery, connection);
        var totalCount = (int)await countCommand.ExecuteScalarAsync()!;

        var offset = (page - 1) * pageSize;
        var query = "SELECT * FROM Drivers WHERE IsActive = 0 ORDER BY DriverId DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@Offset", offset);
        command.Parameters.AddWithValue("@PageSize", pageSize);

        var drivers = new List<Driver>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            drivers.Add(MapToDriver(reader));

        return (drivers, totalCount);
    }

    public async Task<Driver?> GetByIdAsync(int id)
    {
        using var connection = GetConnection();
        await connection.OpenAsync();

        var query = "SELECT * FROM Drivers WHERE DriverId = @DriverId AND IsActive = 1";
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@DriverId", id);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync()) return MapToDriver(reader);
        return null;
    }

    public async Task<Driver?> GetByLicenseNumberAsync(string licenseNumber)
    {
        using var connection = GetConnection();
        await connection.OpenAsync();

        var query = "SELECT * FROM Drivers WHERE LicenseNumber = @LicenseNumber";
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@LicenseNumber", licenseNumber);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync()) return MapToDriver(reader);
        return null;
    }

    public async Task<Driver> UpdateAsync(Driver driver)
    {
        using var connection = GetConnection();
        await connection.OpenAsync();

        var query = @"
            UPDATE Drivers SET
                UserId = @UserId, FullName = @FullName, LicenseNumber = @LicenseNumber,
                LicenseExpiry = @LicenseExpiry, PhoneNumber = @PhoneNumber,
                MaxWorkingHoursPerDay = @MaxWorkingHoursPerDay, UpdatedAt = GETUTCDATE()
            WHERE DriverId = @DriverId AND IsActive = 1";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@DriverId", driver.DriverId);
        command.Parameters.AddWithValue("@UserId", driver.UserId);
        command.Parameters.AddWithValue("@FullName", driver.FullName);
        command.Parameters.AddWithValue("@LicenseNumber", driver.LicenseNumber);
        command.Parameters.AddWithValue("@LicenseExpiry", driver.LicenseExpiry);
        command.Parameters.AddWithValue("@PhoneNumber", driver.PhoneNumber);
        command.Parameters.AddWithValue("@MaxWorkingHoursPerDay", driver.MaxWorkingHoursPerDay);

        await command.ExecuteNonQueryAsync();
        return driver;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = GetConnection();
        await connection.OpenAsync();

        var query = "UPDATE Drivers SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE DriverId = @DriverId AND IsActive = 1";
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@DriverId", id);

        return await command.ExecuteNonQueryAsync() > 0;
    }

    public async Task<IEnumerable<Driver>> GetAvailableAsync()
    {
        using var connection = GetConnection();
        await connection.OpenAsync();

        var query = "SELECT * FROM Drivers WHERE IsActive = 1 AND CurrentWorkingHoursToday < MaxWorkingHoursPerDay";
        using var command = new SqlCommand(query, connection);

        var drivers = new List<Driver>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            drivers.Add(MapToDriver(reader));

        return drivers;
    }

    public async Task<bool> UpdateWorkingHoursAsync(int id, double hoursToAdd)
    {
        using var connection = GetConnection();
        await connection.OpenAsync();

        var query = "UPDATE Drivers SET CurrentWorkingHoursToday = CurrentWorkingHoursToday + @Hours, UpdatedAt = GETUTCDATE() WHERE DriverId = @DriverId AND IsActive = 1";
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@DriverId", id);
        command.Parameters.AddWithValue("@Hours", hoursToAdd);

        return await command.ExecuteNonQueryAsync() > 0;
    }

    private static Driver MapToDriver(SqlDataReader reader) => new Driver
    {
        DriverId = (int)reader["DriverId"],
        UserId = (string)reader["UserId"],
        FullName = (string)reader["FullName"],
        LicenseNumber = (string)reader["LicenseNumber"],
        LicenseExpiry = (string)reader["LicenseExpiry"],
        PhoneNumber = (string)reader["PhoneNumber"],
        MaxWorkingHoursPerDay = (double)reader["MaxWorkingHoursPerDay"],
        CurrentWorkingHoursToday = (double)reader["CurrentWorkingHoursToday"],
        IsActive = (bool)reader["IsActive"],
        CreatedAt = (DateTime)reader["CreatedAt"],
        UpdatedAt = (DateTime)reader["UpdatedAt"]
    };
}
