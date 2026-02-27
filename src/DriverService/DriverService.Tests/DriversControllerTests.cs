using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using DriverService.API.Controllers;
using DriverService.API.Models;
using Xunit;

namespace DriverService.Tests;

/// <summary>
/// Tests for DriversController.
/// Note: The controller is a placeholder implementation with no real service injection.
/// These tests verify the HTTP contract and response shape. Richer tests will follow
/// when the real service layer is introduced.
/// </summary>
public class DriversControllerTests
{
    private readonly DriversController _controller;

    public DriversControllerTests()
    {
        _controller = new DriversController();
    }

    // ---------- GET /api/drivers ----------

    [Fact]
    public void GetDrivers_ReturnsEmptyList_WithCorrectStructure()
    {
        // Act
        var result = _controller.GetDrivers();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        Assert.NotNull(ok.Value);

        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);

        // data should be an empty list (no drivers in placeholder)
        var dataJson = GetRawProperty(ok.Value, "data");
        Assert.NotNull(dataJson);
        var dataArray = JsonSerializer.Deserialize<List<JsonElement>>(dataJson!);
        Assert.NotNull(dataArray);
        Assert.Empty(dataArray);
    }

    [Fact]
    public void GetDrivers_WithCustomPagination_Returns200()
    {
        // Act
        var result = _controller.GetDrivers(page: 2, pageSize: 5);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);
    }

    // ---------- GET /api/drivers/{id} ----------

    [Fact]
    public void GetDriver_ReturnsDriverById()
    {
        // Act
        var result = _controller.GetDriver(42);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        Assert.NotNull(ok.Value);

        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);

        // The placeholder returns a Driver with the requested ID
        var dataJson = GetRawProperty(ok.Value, "data");
        Assert.NotNull(dataJson);
        var driver = JsonSerializer.Deserialize<JsonElement>(dataJson!, CaseInsensitiveOptions);
        Assert.Equal(42, GetInt32Property(driver, "driverid"));
    }

    [Fact]
    public void GetDriver_ReturnsDriverWithExpectedFields()
    {
        // Act
        var result = _controller.GetDriver(1);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var dataJson = GetRawProperty(ok.Value, "data");
        Assert.NotNull(dataJson);

        var driver = JsonSerializer.Deserialize<JsonElement>(dataJson!, CaseInsensitiveOptions);
        // Verify all required fields are present and non-empty (case-insensitive property search)
        Assert.True(TryGetCaseInsensitive(driver, "driverid", out _));
        Assert.True(TryGetCaseInsensitive(driver, "fullname", out var fullName));
        Assert.NotEmpty(fullName.GetString()!);
        Assert.True(TryGetCaseInsensitive(driver, "licensenumber", out _));
    }

    // ---------- POST /api/drivers ----------

    [Fact]
    public void CreateDriver_ReturnsStaticDriver()
    {
        // Arrange
        var newDriver = CreateValidDriver();

        // Act
        var result = _controller.CreateDriver(newDriver);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);

        var success = GetPropertyValue<bool>(objectResult.Value, "success");
        Assert.True(success);

        // Placeholder always assigns DriverId = 1
        var dataJson = GetRawProperty(objectResult.Value, "data");
        Assert.NotNull(dataJson);
        var driver = JsonSerializer.Deserialize<JsonElement>(dataJson!, CaseInsensitiveOptions);
        Assert.Equal(1, GetInt32Property(driver, "driverid"));
    }

    [Fact]
    public void CreateDriver_ResponseContainsDriverData()
    {
        // Arrange
        var newDriver = CreateValidDriver();
        newDriver.FullName = "Jane Smith";

        // Act
        var result = _controller.CreateDriver(newDriver);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, objectResult.StatusCode);

        var dataJson = GetRawProperty(objectResult.Value, "data");
        Assert.NotNull(dataJson);
        var driver = JsonSerializer.Deserialize<JsonElement>(dataJson!, CaseInsensitiveOptions);
        Assert.Equal("Jane Smith", GetStringProperty(driver, "fullname"));
    }

    // ---------- GET /api/drivers/available ----------

    [Fact]
    public void GetAvailableDrivers_ReturnsEmptyList()
    {
        // Act
        var result = _controller.GetAvailableDrivers();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        Assert.NotNull(ok.Value);

        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);

        // Placeholder returns an empty list
        var dataJson = GetRawProperty(ok.Value, "data");
        Assert.NotNull(dataJson);
        var dataArray = JsonSerializer.Deserialize<List<JsonElement>>(dataJson!);
        Assert.NotNull(dataArray);
        Assert.Empty(dataArray);
    }

    // ---------- PUT /api/drivers/{id} ----------

    [Fact]
    public void UpdateDriver_ExistingId_Returns200()
    {
        // Arrange
        var driver = CreateValidDriver();
        driver.FullName = "Updated Name";

        // Act
        var result = _controller.UpdateDriver(7, driver);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);

        // Placeholder assigns the provided ID to the driver
        var dataJson = GetRawProperty(ok.Value, "data");
        Assert.NotNull(dataJson);
        var updated = JsonSerializer.Deserialize<JsonElement>(dataJson!, CaseInsensitiveOptions);
        Assert.Equal(7, GetInt32Property(updated, "driverid"));
    }

    // ---------- PUT /api/drivers/{id}/hours ----------

    [Fact]
    public void UpdateWorkingHours_Returns200WithSuccessMessage()
    {
        // Arrange
        var request = new UpdateHoursDto { HoursToAdd = 4.5 };

        // Act
        var result = _controller.UpdateWorkingHours(3, request);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);
    }

    // ---------- DELETE /api/drivers/{id} ----------

    [Fact]
    public void DeleteDriver_Returns200WithSuccessMessage()
    {
        // Act
        var result = _controller.DeleteDriver(5);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);
    }

    // ---------- Helpers ----------

    private static Driver CreateValidDriver() => new()
    {
        KeycloakUserId = "kc-user-001",
        FullName = "John Doe",
        LicenseNumber = "LIC-12345",
        LicenseExpiry = "2027-12-31",
        PhoneNumber = "+94771234567",
        MaxWorkingHoursPerDay = 8.0,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    // JSON options for case-insensitive property matching
    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static T? GetPropertyValue<T>(object? obj, string propertyName)
    {
        if (obj == null) return default;

        var json = JsonSerializer.Serialize(obj);
        using var doc = JsonDocument.Parse(json);

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (prop.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                return JsonSerializer.Deserialize<T>(prop.Value.GetRawText());
        }

        return default;
    }

    private static string? GetRawProperty(object? obj, string propertyName)
    {
        if (obj == null) return null;

        var json = JsonSerializer.Serialize(obj);
        using var doc = JsonDocument.Parse(json);

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (prop.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                return prop.Value.GetRawText();
        }

        return null;
    }

    private static bool TryGetCaseInsensitive(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var prop in element.EnumerateObject())
        {
            if (prop.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    private static int GetInt32Property(JsonElement element, string propertyName)
    {
        foreach (var prop in element.EnumerateObject())
        {
            if (prop.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                return prop.Value.GetInt32();
        }
        throw new KeyNotFoundException($"Property '{propertyName}' not found in JSON element.");
    }

    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        foreach (var prop in element.EnumerateObject())
        {
            if (prop.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                return prop.Value.GetString();
        }
        return null;
    }
}
