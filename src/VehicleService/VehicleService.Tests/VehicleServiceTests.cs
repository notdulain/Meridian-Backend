using Moq;
using VehicleService.API.Models;
using VehicleService.API.Repositories;
using VehicleService.API.Services;
using Xunit;

namespace VehicleService.Tests;

public class VehicleServiceTests
{
    private readonly Mock<IVehicleRepository> _repositoryMock;
    private readonly API.Services.VehicleService _service;

    public VehicleServiceTests()
    {
        _repositoryMock = new Mock<IVehicleRepository>();
        _service = new API.Services.VehicleService(_repositoryMock.Object);
    }

    // ---------- CreateVehicleAsync ----------

    [Fact]
    public async Task CreateVehicleAsync_CallsRepository()
    {
        // Arrange
        var vehicle = CreateValidVehicle();
        var createdVehicle = CreateValidVehicle();
        createdVehicle.VehicleId = 1;

        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Vehicle>()))
            .ReturnsAsync(createdVehicle);

        // Act
        var result = await _service.CreateVehicleAsync(vehicle);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.VehicleId);
        _repositoryMock.Verify(r => r.CreateAsync(vehicle), Times.Once);
    }

    [Fact]
    public async Task CreateVehicleAsync_ThrowsArgumentException_WhenCapacityKgIsZeroOrLess()
    {
        // Arrange
        var vehicle = CreateValidVehicle();
        vehicle.CapacityKg = 0;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateVehicleAsync(vehicle));
        Assert.Equal("Capacity (Kg) must be greater than zero.", ex.Message);
        
        vehicle.CapacityKg = -10;
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateVehicleAsync(vehicle));

        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Vehicle>()), Times.Never);
    }

    [Fact]
    public async Task CreateVehicleAsync_ThrowsArgumentException_WhenCurrentLocationIsMissing()
    {
        var vehicle = CreateValidVehicle();
        vehicle.CurrentLocation = " ";

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateVehicleAsync(vehicle));

        Assert.Equal("Current location is required.", ex.Message);
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Vehicle>()), Times.Never);
    }

    [Fact]
    public async Task CreateVehicleAsync_ThrowsArgumentException_WhenCapacityM3IsZeroOrLess()
    {
        // Arrange
        var vehicle = CreateValidVehicle();
        vehicle.CapacityM3 = 0;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateVehicleAsync(vehicle));
        Assert.Equal("Capacity (M3) must be greater than zero.", ex.Message);

        vehicle.CapacityM3 = -5;
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateVehicleAsync(vehicle));

        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Vehicle>()), Times.Never);
    }

    [Fact]
    public async Task CreateVehicleAsync_ThrowsArgumentException_WhenPlateNumberAlreadyExists()
    {
        // Arrange
        var vehicle = CreateValidVehicle();
        vehicle.PlateNumber = "DUPLICATE-123";

        // Setup the mock to return an existing vehicle for this plate number
        _repositoryMock
            .Setup(r => r.GetByPlateNumberAsync("DUPLICATE-123"))
            .ReturnsAsync(new Vehicle { PlateNumber = "DUPLICATE-123", Make = "Ford", Model = "Fiesta", Status = "Available" });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateVehicleAsync(vehicle));
        Assert.Equal("A vehicle with plate number 'DUPLICATE-123' already exists.", ex.Message);

        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Vehicle>()), Times.Never);
    }

    // ---------- UpdateVehicleAsync ----------

    [Fact]
    public async Task UpdateVehicleAsync_ThrowsArgumentException_WhenCapacityKgIsZeroOrLess()
    {
        // Arrange
        var vehicle = CreateValidVehicle();
        vehicle.CapacityKg = 0;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateVehicleAsync(1, vehicle));
        Assert.Equal("Capacity (Kg) must be greater than zero.", ex.Message);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Vehicle>()), Times.Never);
    }

    [Fact]
    public async Task UpdateVehicleAsync_ThrowsArgumentException_WhenCurrentLocationIsMissing()
    {
        var vehicle = CreateValidVehicle();
        vehicle.CurrentLocation = string.Empty;

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateVehicleAsync(1, vehicle));

        Assert.Equal("Current location is required.", ex.Message);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Vehicle>()), Times.Never);
    }

    [Fact]
    public async Task UpdateVehicleAsync_ThrowsArgumentException_WhenCapacityM3IsZeroOrLess()
    {
        // Arrange
        var vehicle = CreateValidVehicle();
        vehicle.CapacityM3 = -1;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateVehicleAsync(1, vehicle));
        Assert.Equal("Capacity (M3) must be greater than zero.", ex.Message);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Vehicle>()), Times.Never);
    }

    [Fact]
    public async Task UpdateVehicleAsync_ThrowsArgumentException_WhenStatusIsInvalid()
    {
        // Arrange
        var vehicle = CreateValidVehicle();
        vehicle.Status = "Broken";

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateVehicleAsync(1, vehicle));
        Assert.Contains("Invalid status", ex.Message);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Vehicle>()), Times.Never);
    }

    [Fact]
    public async Task UpdateVehicleAsync_ThrowsArgumentException_WhenPlateNumberBelongsToAnotherVehicle()
    {
        // Arrange
        var vehicle = CreateValidVehicle();
        vehicle.PlateNumber = "TAKEN-999";

        // A different vehicle already has this plate number
        _repositoryMock
            .Setup(r => r.GetByPlateNumberAsync("TAKEN-999"))
            .ReturnsAsync(new Vehicle { VehicleId = 99, PlateNumber = "TAKEN-999", Make = "Honda", Model = "Civic", Status = "Available" });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateVehicleAsync(1, vehicle));
        Assert.Equal("A vehicle with plate number 'TAKEN-999' already exists.", ex.Message);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Vehicle>()), Times.Never);
    }

    [Fact]
    public async Task UpdateVehicleAsync_AllowsSamePlateNumber_WhenBelongsToSameVehicle()
    {
        // Arrange — vehicle is updating itself, plate check should NOT block it
        var vehicle = CreateValidVehicle();
        vehicle.VehicleId = 5;
        vehicle.PlateNumber = "SAME-001";

        var updated = CreateValidVehicle();
        updated.VehicleId = 5;

        _repositoryMock
            .Setup(r => r.GetByPlateNumberAsync("SAME-001"))
            .ReturnsAsync(new Vehicle { VehicleId = 5, PlateNumber = "SAME-001", Make = "Toyota", Model = "Hilux", Status = "Available" });

        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Vehicle>()))
            .ReturnsAsync(updated);

        // Act
        var result = await _service.UpdateVehicleAsync(5, vehicle);

        // Assert
        Assert.NotNull(result);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Vehicle>()), Times.Once);
    }

    // ---------- GetVehicleByIdAsync ----------

    [Fact]
    public async Task GetVehicleByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Vehicle?)null);

        // Act
        var result = await _service.GetVehicleByIdAsync(999);

        // Assert
        Assert.Null(result);
        _repositoryMock.Verify(r => r.GetByIdAsync(999), Times.Once);
    }

    [Fact]
    public async Task GetVehicleByIdAsync_ReturnsVehicle_WhenFound()
    {
        // Arrange
        var vehicle = CreateValidVehicle();
        vehicle.VehicleId = 5;

        _repositoryMock
            .Setup(r => r.GetByIdAsync(5))
            .ReturnsAsync(vehicle);

        // Act
        var result = await _service.GetVehicleByIdAsync(5);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.VehicleId);
        Assert.Equal("ABC-123", result.PlateNumber);
        _repositoryMock.Verify(r => r.GetByIdAsync(5), Times.Once);
    }

    // ---------- UpdateVehicleAsync ----------

    [Fact]
    public async Task UpdateVehicleAsync_SetsCorrectId()
    {
        // Arrange
        var vehicle = CreateValidVehicle();
        var updatedVehicle = CreateValidVehicle();
        updatedVehicle.VehicleId = 10;

        _repositoryMock
            .Setup(r => r.UpdateAsync(It.Is<Vehicle>(v => v.VehicleId == 10)))
            .ReturnsAsync(updatedVehicle);

        // Act
        var result = await _service.UpdateVehicleAsync(10, vehicle);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.VehicleId);
        _repositoryMock.Verify(r => r.UpdateAsync(It.Is<Vehicle>(v => v.VehicleId == 10)), Times.Once);
    }

    [Fact]
    public async Task UpdateVehicleAsync_PreservesOtherProperties()
    {
        // Arrange
        var vehicle = CreateValidVehicle();
        vehicle.PlateNumber = "XYZ-999";
        vehicle.Make = "Ford";
        var updatedVehicle = CreateValidVehicle();
        updatedVehicle.VehicleId = 7;
        updatedVehicle.PlateNumber = "XYZ-999";
        updatedVehicle.Make = "Ford";

        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Vehicle>()))
            .ReturnsAsync(updatedVehicle);

        // Act
        var result = await _service.UpdateVehicleAsync(7, vehicle);

        // Assert
        Assert.Equal(7, result.VehicleId);
        Assert.Equal("XYZ-999", result.PlateNumber);
        Assert.Equal("Ford", result.Make);
    }

    // ---------- DeleteVehicleAsync ----------

    [Fact]
    public async Task DeleteVehicleAsync_ReturnsTrue_WhenSuccessful()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.DeleteAsync(5))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteVehicleAsync(5);

        // Assert
        Assert.True(result);
        _repositoryMock.Verify(r => r.DeleteAsync(5), Times.Once);
    }

    [Fact]
    public async Task DeleteVehicleAsync_ReturnsFalse_WhenNotFound()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.DeleteAsync(It.IsAny<int>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.DeleteVehicleAsync(999);

        // Assert
        Assert.False(result);
        _repositoryMock.Verify(r => r.DeleteAsync(999), Times.Once);
    }

    // ---------- UpdateVehicleStatusAsync ----------

    [Fact]
    public async Task UpdateVehicleStatusAsync_ReturnsTrue_WhenSuccessful()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.UpdateStatusAsync(3, "Maintenance"))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateVehicleStatusAsync(3, "Maintenance");

        // Assert
        Assert.True(result);
        _repositoryMock.Verify(r => r.UpdateStatusAsync(3, "Maintenance"), Times.Once);
    }

    [Fact]
    public async Task UpdateVehicleStatusAsync_ReturnsFalse_WhenNotFound()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.UpdateStatusAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.UpdateVehicleStatusAsync(999, "Maintenance");

        // Assert
        Assert.False(result);
    }

    // ---------- GetAvailableVehiclesAsync ----------

    [Fact]
    public async Task GetAvailableVehiclesAsync_ReturnsAvailableVehicles()
    {
        // Arrange
        var vehicle1 = CreateValidVehicle();
        vehicle1.VehicleId = 1;
        vehicle1.Status = "Available";
        
        var vehicle2 = CreateValidVehicle();
        vehicle2.VehicleId = 2;
        vehicle2.Status = "Available";
        vehicle2.PlateNumber = "DEF-456";
        
        var availableVehicles = new List<Vehicle> { vehicle1, vehicle2 };

        _repositoryMock
            .Setup(r => r.GetAvailableAsync())
            .ReturnsAsync(availableVehicles);

        // Act
        var result = await _service.GetAvailableVehiclesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, v => Assert.Equal("Available", v.Status));
    }

    [Fact]
    public async Task GetAvailableVehiclesAsync_ReturnsEmptyList_WhenNoneAvailable()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetAvailableAsync())
            .ReturnsAsync(new List<Vehicle>());

        // Act
        var result = await _service.GetAvailableVehiclesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // ---------- GetVehiclesAsync ----------

    [Fact]
    public async Task GetVehiclesAsync_ReturnsPaginatedResults()
    {
        // Arrange
        var vehicle1 = CreateValidVehicle();
        vehicle1.VehicleId = 1;
        
        var vehicle2 = CreateValidVehicle();
        vehicle2.VehicleId = 2;
        vehicle2.PlateNumber = "GHI-789";
        
        var vehicles = new List<Vehicle> { vehicle1, vehicle2 };

        _repositoryMock
            .Setup(r => r.GetAllAsync(1, 10, null, null))
            .ReturnsAsync((vehicles, 2));

        // Act
        var (resultVehicles, totalCount) = await _service.GetVehiclesAsync(1, 10, null, null);

        // Assert
        Assert.NotNull(resultVehicles);
        Assert.Equal(2, resultVehicles.Count());
        Assert.Equal(2, totalCount);
        _repositoryMock.Verify(r => r.GetAllAsync(1, 10, null, null), Times.Once);
    }

    [Fact]
    public async Task GetVehiclesAsync_WithStatusFilter_ReturnsFilteredResults()
    {
        // Arrange
        var vehicle1 = CreateValidVehicle();
        vehicle1.VehicleId = 1;
        vehicle1.Status = "Maintenance";
        
        var vehicles = new List<Vehicle> { vehicle1 };

        _repositoryMock
            .Setup(r => r.GetAllAsync(1, 10, "Maintenance", null))
            .ReturnsAsync((vehicles, 1));

        // Act
        var (resultVehicles, totalCount) = await _service.GetVehiclesAsync(1, 10, "Maintenance", null);

        // Assert
        Assert.Single(resultVehicles);
        Assert.Equal(1, totalCount);
        Assert.All(resultVehicles, v => Assert.Equal("Maintenance", v.Status));
    }

    [Fact]
    public async Task GetVehiclesAsync_WithIsActiveFilter_ReturnsFilteredResults()
    {
        // Arrange
        var vehicle1 = CreateValidVehicle();
        vehicle1.VehicleId = 1;
        vehicle1.Status = "Available";
        
        var vehicles = new List<Vehicle> { vehicle1 };

        _repositoryMock
            .Setup(r => r.GetAllAsync(1, 10, null, true))
            .ReturnsAsync((vehicles, 1));

        // Act
        var (resultVehicles, totalCount) = await _service.GetVehiclesAsync(1, 10, null, true);

        // Assert
        Assert.Single(resultVehicles);
        Assert.Equal(1, totalCount);
        Assert.All(resultVehicles, v => Assert.NotEqual("Retired", v.Status));
    }

    // ---------- Helpers ----------

    private static Vehicle CreateValidVehicle() => new()
    {
        PlateNumber = "ABC-123",
        Make = "Toyota",
        Model = "Hilux",
        CurrentLocation = "Colombo Fort",
        Year = 2023,
        CapacityKg = 1000,
        CapacityM3 = 5.5,
        FuelEfficiencyKmPerLitre = 12.5,
        Status = "Available",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
