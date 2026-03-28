using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;
using System.Text.Json;
using DriverService.API.Controllers;
using DriverService.API.Models;
using DriverService.API.Services;
using Xunit;

namespace DriverService.Tests;

public class DriversControllerTests
{
    private readonly Mock<IDriverService> _serviceMock;
    private readonly DriversController _controller;

    public DriversControllerTests()
    {
        _serviceMock = new Mock<IDriverService>();
        _controller = new DriversController(_serviceMock.Object);
    }

    // ---------- Helpers ----------

    private static Driver CreateValidDriver() => new()
    {
        UserId = "user-001",
        FullName = "John Doe",
        LicenseNumber = "LIC-12345",
        LicenseExpiry = DateTime.UtcNow.AddYears(2).ToString("yyyy-MM-dd"),
        PhoneNumber = "+94771234567",
        MaxWorkingHoursPerDay = 8.0,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static T? GetPropertyValue<T>(object? obj, string propertyName)
    {
        if (obj == null) return default;
        var json = JsonSerializer.Serialize(obj);
        using var doc = JsonDocument.Parse(json);
        foreach (var prop in doc.RootElement.EnumerateObject())
            if (prop.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                return JsonSerializer.Deserialize<T>(prop.Value.GetRawText());
        return default;
    }

    // ---------- POST /api/Drivers ----------

    [Fact]
    public async Task CreateDriver_Returns201_WhenValid()
    {
        // Arrange
        var driver = CreateValidDriver();
        var created = CreateValidDriver();
        created.DriverId = 1;

        _serviceMock
            .Setup(s => s.CreateDriverAsync(It.IsAny<Driver>()))
            .ReturnsAsync(created);

        // Act
        var result = await _controller.CreateDriver(driver);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, objectResult.StatusCode);
        Assert.True(GetPropertyValue<bool>(objectResult.Value, "success"));
    }

    [Fact]
    public async Task CreateDriver_Returns400_WhenServiceThrows()
    {
        // Arrange
        var driver = CreateValidDriver();
        _serviceMock
            .Setup(s => s.CreateDriverAsync(It.IsAny<Driver>()))
            .ThrowsAsync(new ArgumentException("A driver with license number 'LIC-12345' already exists."));

        // Act
        var result = await _controller.CreateDriver(driver);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.False(GetPropertyValue<bool>(badRequest.Value, "success"));
    }

    // ---------- GET /api/Drivers ----------

    [Fact]
    public async Task GetDrivers_Returns200_WithPaginatedResults()
    {
        // Arrange
        var drivers = new List<Driver> { CreateValidDriver(), CreateValidDriver() };
        _serviceMock
            .Setup(s => s.GetDriversAsync(1, 10))
            .ReturnsAsync((drivers, 2));

        // Act
        var result = await _controller.GetDrivers(1, 10);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.True(GetPropertyValue<bool>(ok.Value, "success"));
    }

    [Fact]
    public async Task GetDeletedDrivers_Returns200_WithPaginatedResults()
    {
        // Arrange
        var drivers = new List<Driver> { CreateValidDriver() };
        _serviceMock
            .Setup(s => s.GetDeletedDriversAsync(1, 10))
            .ReturnsAsync((drivers, 1));

        // Act
        var result = await _controller.GetDeletedDrivers(1, 10);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.True(GetPropertyValue<bool>(ok.Value, "success"));
        Assert.NotNull(GetPropertyValue<object>(ok.Value, "meta"));
    }

    // ---------- GET /api/Drivers/{id} ----------

    [Fact]
    public async Task GetDriver_Returns200_WhenFound()
    {
        // Arrange
        var driver = CreateValidDriver();
        driver.DriverId = 5;
        _serviceMock.Setup(s => s.GetDriverByIdAsync(5)).ReturnsAsync(driver);

        // Act
        var result = await _controller.GetDriver(5);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.True(GetPropertyValue<bool>(ok.Value, "success"));
    }

    [Fact]
    public async Task GetDriver_Returns404_WhenNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetDriverByIdAsync(999)).ReturnsAsync((Driver?)null);

        // Act
        var result = await _controller.GetDriver(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetDriver_Returns404_WhenDriverIsSoftDeleted()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetDriverByIdAsync(5)).ReturnsAsync((Driver?)null);

        // Act
        var result = await _controller.GetDriver(5);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetMyProfile_Returns200_WhenDriverProfileExists()
    {
        var driver = CreateValidDriver();
        driver.DriverId = 2005;

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "42"),
                ], "test"))
            }
        };

        _serviceMock
            .Setup(s => s.GetDriverByUserIdAsync("42"))
            .ReturnsAsync(driver);

        var result = await _controller.GetMyProfile();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.True(GetPropertyValue<bool>(ok.Value, "success"));
        var data = GetPropertyValue<Driver>(ok.Value, "data");
        Assert.NotNull(data);
        Assert.Equal(2005, data!.DriverId);
    }

    [Fact]
    public async Task GetMyProfile_Returns404_WhenDriverProfileMissing()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim("sub", "42"),
                ], "test"))
            }
        };

        _serviceMock
            .Setup(s => s.GetDriverByUserIdAsync("42"))
            .ReturnsAsync((Driver?)null);

        var result = await _controller.GetMyProfile();

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ---------- PUT /api/Drivers/{id} ----------

    [Fact]
    public async Task UpdateDriver_Returns200_WhenValid()
    {
        // Arrange
        var driver = CreateValidDriver();
        var existing = CreateValidDriver();
        existing.DriverId = 7;

        _serviceMock.Setup(s => s.GetDriverByIdAsync(7)).ReturnsAsync(existing);
        _serviceMock.Setup(s => s.UpdateDriverAsync(7, It.IsAny<Driver>())).ReturnsAsync(existing);

        // Act
        var result = await _controller.UpdateDriver(7, driver);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.True(GetPropertyValue<bool>(ok.Value, "success"));
    }

    [Fact]
    public async Task UpdateDriver_Returns404_WhenNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetDriverByIdAsync(999)).ReturnsAsync((Driver?)null);

        // Act
        var result = await _controller.UpdateDriver(999, CreateValidDriver());

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateDriver_Returns404_WhenDriverIsSoftDeleted()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetDriverByIdAsync(7)).ReturnsAsync((Driver?)null);

        // Act
        var result = await _controller.UpdateDriver(7, CreateValidDriver());

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
        _serviceMock.Verify(s => s.UpdateDriverAsync(It.IsAny<int>(), It.IsAny<Driver>()), Times.Never);
    }

    [Fact]
    public async Task UpdateDriver_Returns400_WhenServiceThrowsValidationError()
    {
        // Arrange
        var driver = CreateValidDriver();
        var existing = CreateValidDriver();
        existing.DriverId = 7;

        _serviceMock.Setup(s => s.GetDriverByIdAsync(7)).ReturnsAsync(existing);
        _serviceMock
            .Setup(s => s.UpdateDriverAsync(7, It.IsAny<Driver>()))
            .ThrowsAsync(new ArgumentException("License number is required."));

        // Act
        var result = await _controller.UpdateDriver(7, driver);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.False(GetPropertyValue<bool>(badRequest.Value, "success"));
        Assert.Equal("Failed to update driver", GetPropertyValue<string>(badRequest.Value, "message"));
        var errors = GetPropertyValue<string[]>(badRequest.Value, "errors");
        Assert.NotNull(errors);
        Assert.Contains("License number is required.", errors!);
    }

    // ---------- PUT /api/Drivers/{id}/hours ----------

    [Fact]
    public async Task UpdateWorkingHours_Returns200_WhenValid()
    {
        // Arrange
        _serviceMock.Setup(s => s.UpdateWorkingHoursAsync(3, 4.5)).ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateWorkingHours(3, new UpdateHoursDto { HoursToAdd = 4.5 });

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.True(GetPropertyValue<bool>(ok.Value, "success"));
    }

    [Fact]
    public async Task UpdateWorkingHours_Returns404_WhenDriverNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.UpdateWorkingHoursAsync(999, It.IsAny<double>())).ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateWorkingHours(999, new UpdateHoursDto { HoursToAdd = 2.0 });

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ---------- DELETE /api/Drivers/{id} ----------

    [Fact]
    public async Task DeleteDriver_Returns200_WhenDeleted()
    {
        // Arrange
        _serviceMock.Setup(s => s.DeleteDriverAsync(5)).ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteDriver(5);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.True(GetPropertyValue<bool>(ok.Value, "success"));
    }

    [Fact]
    public async Task DeleteDriver_Returns404_WhenNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.DeleteDriverAsync(999)).ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteDriver(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ---------- GET /api/Drivers/available ----------

    [Fact]
    public async Task GetAvailableDrivers_Returns200_WithList()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.GetAvailableDriversAsync())
            .ReturnsAsync(new List<Driver> { CreateValidDriver() });

        // Act
        var result = await _controller.GetAvailableDrivers();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.True(GetPropertyValue<bool>(ok.Value, "success"));
    }
}
