using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Text.Json;
using VehicleService.API.Controllers;
using VehicleService.API.Models;
using VehicleService.API.Services;
using Xunit;

namespace VehicleService.Tests;

public class VehiclesControllerTests
{
    private readonly Mock<IVehicleService> _serviceMock;
    private readonly VehiclesController _controller;

    public VehiclesControllerTests()
    {
        _serviceMock = new Mock<IVehicleService>();
        _controller = new VehiclesController(_serviceMock.Object);
    }

    // ---------- POST /api/vehicles ----------

    [Fact]
    public async Task CreateVehicle_ValidVehicle_Returns201()
    {
        // Arrange
        var vehicle = CreateValidVehicle();
        var createdVehicle = CreateValidVehicle();
        createdVehicle.VehicleId = 1;

        _serviceMock
            .Setup(s => s.CreateVehicleAsync(It.IsAny<Vehicle>()))
            .ReturnsAsync(createdVehicle);

        // Act
        var result = await _controller.CreateVehicle(vehicle);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, objectResult.StatusCode);

        var success = GetPropertyValue<bool>(objectResult.Value, "success");
        Assert.True(success);
        Assert.NotNull(objectResult.Value);
    }

    [Fact]
    public async Task CreateVehicle_ServiceThrowsException_Returns400()
    {
        // Arrange
        var vehicle = CreateValidVehicle();

        _serviceMock
            .Setup(s => s.CreateVehicleAsync(It.IsAny<Vehicle>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateVehicle(vehicle);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
        var success = GetPropertyValue<bool>(badRequest.Value, "success");
        Assert.False(success);
    }

    // ---------- GET /api/vehicles/{id} ----------

    [Fact]
    public async Task GetVehicle_ExistingId_Returns200()
    {
        // Arrange
        var vehicle = CreateValidVehicle();
        vehicle.VehicleId = 5;

        _serviceMock
            .Setup(s => s.GetVehicleByIdAsync(5))
            .ReturnsAsync(vehicle);

        // Act
        var result = await _controller.GetVehicle(5);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);
        var data = GetPropertyValue<object>(ok.Value, "data");
        Assert.NotNull(data);
    }

    [Fact]
    public async Task GetVehicle_NonExistingId_Returns404()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.GetVehicleByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Vehicle?)null);

        // Act
        var result = await _controller.GetVehicle(999);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFound.Value);
        var success = GetPropertyValue<bool>(notFound.Value, "success");
        Assert.False(success);
    }

    [Fact]
    public async Task GetVehicle_ServiceThrowsException_Returns400()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.GetVehicleByIdAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetVehicle(5);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
        var success = GetPropertyValue<bool>(badRequest.Value, "success");
        Assert.False(success);
    }

    // ---------- PUT /api/vehicles/{id} ----------

    [Fact]
    public async Task UpdateVehicle_ExistingId_Returns200()
    {
        // Arrange
        var existingVehicle = CreateValidVehicle();
        existingVehicle.VehicleId = 3;
        var updateVehicle = CreateValidVehicle();
        updateVehicle.PlateNumber = "UPDATED-123";
        var updatedVehicle = CreateValidVehicle();
        updatedVehicle.VehicleId = 3;
        updatedVehicle.PlateNumber = "UPDATED-123";

        _serviceMock
            .Setup(s => s.GetVehicleByIdAsync(3))
            .ReturnsAsync(existingVehicle);

        _serviceMock
            .Setup(s => s.UpdateVehicleAsync(3, It.IsAny<Vehicle>()))
            .ReturnsAsync(updatedVehicle);

        // Act
        var result = await _controller.UpdateVehicle(3, updateVehicle);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);
        var data = GetPropertyValue<object>(ok.Value, "data");
        Assert.NotNull(data);
    }

    [Fact]
    public async Task UpdateVehicle_NonExistingId_Returns404()
    {
        // Arrange
        var updateVehicle = CreateValidVehicle();

        _serviceMock
            .Setup(s => s.GetVehicleByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Vehicle?)null);

        // Act
        var result = await _controller.UpdateVehicle(999, updateVehicle);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFound.Value);
        var success = GetPropertyValue<bool>(notFound.Value, "success");
        Assert.False(success);
    }

    [Fact]
    public async Task UpdateVehicle_ServiceThrowsException_Returns400()
    {
        // Arrange
        var existingVehicle = CreateValidVehicle();
        existingVehicle.VehicleId = 3;
        var updateVehicle = CreateValidVehicle();

        _serviceMock
            .Setup(s => s.GetVehicleByIdAsync(3))
            .ReturnsAsync(existingVehicle);

        _serviceMock
            .Setup(s => s.UpdateVehicleAsync(3, It.IsAny<Vehicle>()))
            .ThrowsAsync(new Exception("Update failed"));

        // Act
        var result = await _controller.UpdateVehicle(3, updateVehicle);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
        var success = GetPropertyValue<bool>(badRequest.Value, "success");
        Assert.False(success);
    }

    // ---------- DELETE /api/vehicles/{id} ----------

    [Fact]
    public async Task DeleteVehicle_ExistingId_Returns200()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.DeleteVehicleAsync(5))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteVehicle(5);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);
    }

    [Fact]
    public async Task DeleteVehicle_NonExistingId_Returns404()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.DeleteVehicleAsync(It.IsAny<int>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteVehicle(999);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFound.Value);
        var success = GetPropertyValue<bool>(notFound.Value, "success");
        Assert.False(success);
    }

    [Fact]
    public async Task DeleteVehicle_ServiceThrowsException_Returns400()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.DeleteVehicleAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Delete failed"));

        // Act
        var result = await _controller.DeleteVehicle(5);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
        var success = GetPropertyValue<bool>(badRequest.Value, "success");
        Assert.False(success);
    }

    // ---------- GET /api/vehicles/available ----------

    [Fact]
    public async Task GetAvailableVehicles_ReturnsListSuccessfully()
    {
        // Arrange
        var vehicle1 = CreateValidVehicle();
        vehicle1.VehicleId = 1;
        vehicle1.Status = "Available";

        var vehicle2 = CreateValidVehicle();
        vehicle2.VehicleId = 2;
        vehicle2.Status = "Available";
        vehicle2.PlateNumber = "ABC-456";

        var availableVehicles = new List<Vehicle> { vehicle1, vehicle2 };

        _serviceMock
            .Setup(s => s.GetAvailableVehiclesAsync())
            .ReturnsAsync(availableVehicles);

        // Act
        var result = await _controller.GetAvailableVehicles();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);
        var data = GetPropertyValue<object>(ok.Value, "data");
        Assert.NotNull(data);
    }

    [Fact]
    public async Task GetAvailableVehicles_EmptyList_Returns200()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.GetAvailableVehiclesAsync())
            .ReturnsAsync(new List<Vehicle>());

        // Act
        var result = await _controller.GetAvailableVehicles();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);
    }

    [Fact]
    public async Task GetAvailableVehicles_ServiceThrowsException_Returns400()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.GetAvailableVehiclesAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAvailableVehicles();

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
        var success = GetPropertyValue<bool>(badRequest.Value, "success");
        Assert.False(success);
    }

    // ---------- GET /api/vehicles ----------

    [Fact]
    public async Task GetVehicles_WithPagination_Returns200()
    {
        // Arrange
        var vehicle1 = CreateValidVehicle();
        vehicle1.VehicleId = 1;

        var vehicle2 = CreateValidVehicle();
        vehicle2.VehicleId = 2;
        vehicle2.PlateNumber = "DEF-456";

        var vehicles = new List<Vehicle> { vehicle1, vehicle2 };

        _serviceMock
            .Setup(s => s.GetVehiclesAsync(1, 10, null, null))
            .ReturnsAsync((vehicles, 2));

        // Act
        var result = await _controller.GetVehicles(1, 10, null, null);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);
    }

    [Fact]
    public async Task GetVehicles_WithStatusFilter_Returns200()
    {
        // Arrange
        var vehicle1 = CreateValidVehicle();
        vehicle1.VehicleId = 1;
        vehicle1.Status = "Available";

        var vehicles = new List<Vehicle> { vehicle1 };

        _serviceMock
            .Setup(s => s.GetVehiclesAsync(1, 10, "Available", null))
            .ReturnsAsync((vehicles, 1));

        // Act
        var result = await _controller.GetVehicles(1, 10, "Available", null);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);
    }

    [Fact]
    public async Task GetVehicles_WithIsActiveFilter_Returns200()
    {
        // Arrange
        var vehicle1 = CreateValidVehicle();
        vehicle1.VehicleId = 1;
        vehicle1.Status = "Available";

        var vehicles = new List<Vehicle> { vehicle1 };

        _serviceMock
            .Setup(s => s.GetVehiclesAsync(1, 10, null, true))
            .ReturnsAsync((vehicles, 1));

        // Act
        var result = await _controller.GetVehicles(1, 10, null, true);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);
    }

    // ---------- PUT /api/vehicles/{id}/status ----------

    [Fact]
    public async Task UpdateStatus_ExistingId_Returns200()
    {
        // Arrange
        var request = new UpdateStatusRequestDto { Status = "Maintenance" };

        _serviceMock
            .Setup(s => s.UpdateVehicleStatusAsync(5, "Maintenance"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateStatus(5, request);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);
    }

    [Fact]
    public async Task UpdateStatus_NonExistingId_Returns404()
    {
        // Arrange
        var request = new UpdateStatusRequestDto { Status = "Maintenance" };

        _serviceMock
            .Setup(s => s.UpdateVehicleStatusAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateStatus(999, request);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFound.Value);
        var success = GetPropertyValue<bool>(notFound.Value, "success");
        Assert.False(success);
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

    private static T? GetPropertyValue<T>(object? obj, string propertyName)
    {
        if (obj == null) return default;

        var json = JsonSerializer.Serialize(obj);
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty(propertyName, out var property))
        {
            return JsonSerializer.Deserialize<T>(property.GetRawText());
        }

        return default;
    }
}
