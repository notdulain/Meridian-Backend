using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using DeliveryService.API.Models;

namespace DeliveryService.API.Repositories;

public class DeliveryRepository
{
    private readonly string _connectionString;

    public DeliveryRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DeliveryDb")
            ?? throw new InvalidOperationException("Connection string 'DeliveryDb' is not configured.");
    }

    public async Task<Delivery> CreateAsync(Delivery delivery, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            INSERT INTO Deliveries (PickupAddress, DeliveryAddress, PackageWeightKg, PackageVolumeM3, Deadline, Status, CreatedBy)
            OUTPUT INSERTED.DeliveryId, INSERTED.CreatedAt, INSERTED.UpdatedAt
            VALUES (@PickupAddress, @DeliveryAddress, @PackageWeightKg, @PackageVolumeM3, @Deadline, @Status, @CreatedBy);
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@PickupAddress", delivery.PickupAddress);
        cmd.Parameters.AddWithValue("@DeliveryAddress", delivery.DeliveryAddress);
        cmd.Parameters.AddWithValue("@PackageWeightKg", delivery.PackageWeightKg);
        cmd.Parameters.AddWithValue("@PackageVolumeM3", delivery.PackageVolumeM3);
        cmd.Parameters.AddWithValue("@Deadline", delivery.Deadline);
        cmd.Parameters.AddWithValue("@Status", delivery.Status);
        cmd.Parameters.AddWithValue("@CreatedBy", delivery.CreatedBy);

        var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException("Insert did not return a row.");

        delivery.Id = reader.GetInt32(0);
        delivery.CreatedAt = reader.GetDateTime(1);
        delivery.UpdatedAt = reader.GetDateTime(2);
        return delivery;
    }
}
