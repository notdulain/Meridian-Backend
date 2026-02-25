using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using DeliveryService.API.Models;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

    public async Task<Delivery?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string deliverySql = """
            SELECT DeliveryId, PickupAddress, DeliveryAddress, PackageWeightKg, PackageVolumeM3,
                   Deadline, Status, AssignedVehicleId, AssignedDriverId, CreatedAt, UpdatedAt, CreatedBy
            FROM Deliveries
            WHERE DeliveryId = @Id;
            """;

        Delivery? delivery = null;

        await using (var cmd = new SqlCommand(deliverySql, connection))
        {
            cmd.Parameters.AddWithValue("@Id", id);
            await using var dr = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await dr.ReadAsync(cancellationToken))
            {
                delivery = new Delivery
                {
                    Id = dr.GetInt32(0),
                    PickupAddress = dr.GetString(1),
                    DeliveryAddress = dr.GetString(2),
                    PackageWeightKg = dr.GetDecimal(3),
                    PackageVolumeM3 = dr.GetDecimal(4),
                    Deadline = dr.GetDateTime(5),
                    Status = dr.GetString(6),
                    AssignedVehicleId = dr.IsDBNull(7) ? null : dr.GetInt32(7),
                    AssignedDriverId = dr.IsDBNull(8) ? null : dr.GetInt32(8),
                    CreatedAt = dr.GetDateTime(9),
                    UpdatedAt = dr.GetDateTime(10),
                    CreatedBy = dr.GetString(11)
                };
            }
        }

        if (delivery is null) return null;

        const string historySql = """
            SELECT StatusHistoryId, PreviousStatus, NewStatus, ChangedAt, ChangedBy, Notes
            FROM StatusHistory
            WHERE DeliveryId = @Id
            ORDER BY ChangedAt ASC;
            """;

        await using (var cmd = new SqlCommand(historySql, connection))
        {
            cmd.Parameters.AddWithValue("@Id", id);
            await using var dr = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await dr.ReadAsync(cancellationToken))
            {
                delivery.StatusHistory.Add(new StatusHistory
                {
                    StatusHistoryId = dr.GetInt32(0),
                    DeliveryId = id,
                    PreviousStatus = dr.IsDBNull(1) ? null : dr.GetString(1),
                    NewStatus = dr.GetString(2),
                    ChangedAt = dr.GetDateTime(3),
                    ChangedBy = dr.GetInt32(4),
                    Notes = dr.IsDBNull(5) ? null : dr.GetString(5)
                });
            }
        }

        return delivery;
    }
}
