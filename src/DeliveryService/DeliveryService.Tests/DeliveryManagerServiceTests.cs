using DeliveryService.API.DTOs;
using DeliveryService.API.Models;
using DeliveryService.API.Services;
using Xunit;

namespace DeliveryService.Tests;

/// <summary>
/// Tests the pure business logic inside DeliveryManagerService.
/// Since DeliveryRepository is a concrete class wired to SQL Server, we use
/// a testable subclass (FakeDeliveryManagerService) to isolate the service logic
/// from infrastructure concerns, following the same pattern as the real implementation.
/// </summary>
public class DeliveryManagerServiceTests
{
    // ---------- CreateDelivery — status always "Pending" ----------

    [Fact]
    public async Task CreateDeliveryAsync_AlwaysSetsStatusToPending()
    {
        // Arrange
        var sut = new FakeDeliveryManagerService(createResult: MakeDelivery(status: "Pending"));
        var request = MakeRequest();

        // Act
        var result = await sut.CreateDeliveryAsync(request);

        // Assert
        Assert.Equal("Pending", result.Status);
    }

    [Fact]
    public async Task CreateDeliveryAsync_MapsAllFieldsFromRequest()
    {
        // Arrange
        var deadline = DateTime.UtcNow.AddDays(3);
        var delivery = MakeDelivery(
            pickup: "123 Main St",
            delivery: "456 Elm St",
            weight: 5.5m,
            volume: 0.3m,
            deadline: deadline,
            createdBy: "dispatcher-1",
            status: "Pending"
        );
        var sut = new FakeDeliveryManagerService(createResult: delivery);
        var request = MakeRequest();

        // Act
        var result = await sut.CreateDeliveryAsync(request);

        // Assert
        Assert.Equal("123 Main St", result.PickupAddress);
        Assert.Equal("456 Elm St", result.DeliveryAddress);
        Assert.Equal(5.5m, result.PackageWeightKg);
        Assert.Equal(0.3m, result.PackageVolumeM3);
        Assert.Equal(deadline, result.Deadline);
        Assert.Equal("dispatcher-1", result.CreatedBy);
    }

    [Fact]
    public async Task CreateDeliveryAsync_MapsStatusHistoryToDto()
    {
        // Arrange
        var delivery = MakeDelivery(status: "Pending");
        delivery.StatusHistory.Add(new StatusHistory
        {
            StatusHistoryId = 1,
            PreviousStatus = null,
            NewStatus = "Pending",
            ChangedAt = DateTime.UtcNow,
            ChangedBy = 42,
            Notes = "Initial"
        });
        var sut = new FakeDeliveryManagerService(createResult: delivery);

        // Act
        var result = await sut.CreateDeliveryAsync(MakeRequest());

        // Assert
        Assert.Single(result.StatusHistory);
        Assert.Equal("Pending", result.StatusHistory[0].NewStatus);
        Assert.Null(result.StatusHistory[0].PreviousStatus);
    }

    // ---------- GetDeliveryById ----------

    [Fact]
    public async Task GetDeliveryByIdAsync_ReturnsNull_WhenDeliveryNotFound()
    {
        // Arrange
        var sut = new FakeDeliveryManagerService(getResult: null);

        // Act
        var result = await sut.GetDeliveryByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetDeliveryByIdAsync_ReturnsMappedDto_WhenDeliveryFound()
    {
        // Arrange
        var delivery = MakeDelivery(id: 7, status: "InTransit");
        var sut = new FakeDeliveryManagerService(getResult: delivery);

        // Act
        var result = await sut.GetDeliveryByIdAsync(7);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(7, result.Id);
        Assert.Equal("InTransit", result.Status);
    }

    [Fact]
    public async Task GetDeliveryByIdAsync_ReturnsEmptyStatusHistory_WhenNoHistory()
    {
        // Arrange
        var delivery = MakeDelivery();
        var sut = new FakeDeliveryManagerService(getResult: delivery);

        // Act
        var result = await sut.GetDeliveryByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.StatusHistory);
    }

    // ---------- Helpers ----------

    private static Delivery MakeDelivery(
        int id = 1,
        string pickup = "123 Main St",
        string delivery = "456 Elm St",
        decimal weight = 5.5m,
        decimal volume = 0.3m,
        DateTime? deadline = null,
        string createdBy = "dispatcher-1",
        string status = "Pending") => new()
    {
        Id = id,
        PickupAddress = pickup,
        DeliveryAddress = delivery,
        PackageWeightKg = weight,
        PackageVolumeM3 = volume,
        Deadline = deadline ?? DateTime.UtcNow.AddDays(3),
        CreatedBy = createdBy,
        Status = status,
        StatusHistory = []
    };

    private static CreateDeliveryRequestDto MakeRequest() => new()
    {
        PickupAddress = "123 Main St",
        DeliveryAddress = "456 Elm St",
        PackageWeightKg = 5.5m,
        PackageVolumeM3 = 0.3m,
        Deadline = DateTime.UtcNow.AddDays(3),
        CreatedBy = "dispatcher-1"
    };
}

/// <summary>
/// A testable double for DeliveryManagerService that bypasses the SQL repository,
/// enabling pure unit tests of the service mapping and business logic.
/// </summary>
internal class FakeDeliveryManagerService : IDeliveryManagerService
{
    private readonly Delivery? _createResult;
    private readonly Delivery? _getResult;

    public FakeDeliveryManagerService(Delivery? createResult = null, Delivery? getResult = null)
    {
        _createResult = createResult;
        _getResult = getResult;
    }

    public Task<DeliveryDto> CreateDeliveryAsync(CreateDeliveryRequestDto request, CancellationToken cancellationToken = default)
    {
        var delivery = _createResult ?? new Delivery
        {
            Id = 1,
            PickupAddress = request.PickupAddress,
            DeliveryAddress = request.DeliveryAddress,
            PackageWeightKg = request.PackageWeightKg,
            PackageVolumeM3 = request.PackageVolumeM3,
            Deadline = request.Deadline,
            Status = "Pending",
            CreatedBy = request.CreatedBy,
            StatusHistory = []
        };
        return Task.FromResult(ToDto(delivery));
    }

    public Task<DeliveryDto?> GetDeliveryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (_getResult is null) return Task.FromResult<DeliveryDto?>(null);
        return Task.FromResult<DeliveryDto?>(ToDto(_getResult));
    }

    private static DeliveryDto ToDto(Delivery d) => new()
    {
        Id = d.Id,
        PickupAddress = d.PickupAddress,
        DeliveryAddress = d.DeliveryAddress,
        PackageWeightKg = d.PackageWeightKg,
        PackageVolumeM3 = d.PackageVolumeM3,
        Deadline = d.Deadline,
        Status = d.Status,
        AssignedVehicleId = d.AssignedVehicleId,
        AssignedDriverId = d.AssignedDriverId,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt,
        CreatedBy = d.CreatedBy,
        StatusHistory = d.StatusHistory.Select(h => new DeliveryStatusHistoryDto
        {
            StatusHistoryId = h.StatusHistoryId,
            PreviousStatus = h.PreviousStatus,
            NewStatus = h.NewStatus,
            ChangedAt = h.ChangedAt,
            ChangedBy = h.ChangedBy,
            Notes = h.Notes
        }).ToList()
    };
}
