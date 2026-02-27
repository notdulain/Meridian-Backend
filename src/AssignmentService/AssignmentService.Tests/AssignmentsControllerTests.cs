using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using AssignmentService.API.Controllers;
using AssignmentService.API.Models;
using Xunit;

namespace AssignmentService.Tests;

/// <summary>
/// Tests for AssignmentsController.
/// Note: The controller is a placeholder implementation. It depends on gRPC clients
/// (VehicleGrpcClient, DriverGrpcClient) and IHttpClientFactory. Since no real gRPC
/// calls are made in the placeholder, these are passed as null. Tests verify the
/// HTTP contract and response shape only.
/// </summary>
public class AssignmentsControllerTests
{
    private readonly AssignmentsController _controller;

    public AssignmentsControllerTests()
    {
        // Placeholder controller — gRPC clients and IHttpClientFactory are unused
        _controller = new AssignmentsController(null!, null!, null!);
    }

    // ---------- GET /api/assignments ----------

    [Fact]
    public void GetAssignments_ReturnsEmptyList()
    {
        // Act
        var result = _controller.GetAssignments();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        Assert.NotNull(ok.Value);

        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);

        var dataJson = GetRawProperty(ok.Value, "data");
        Assert.NotNull(dataJson);
        var dataArray = JsonSerializer.Deserialize<List<JsonElement>>(dataJson!);
        Assert.NotNull(dataArray);
        Assert.Empty(dataArray);
    }

    [Fact]
    public void GetAssignments_WithCustomPagination_Returns200()
    {
        // Act
        var result = _controller.GetAssignments(page: 2, pageSize: 5);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);
    }

    // ---------- GET /api/assignments/{id} ----------

    [Fact]
    public void GetAssignment_ReturnsAssignmentById()
    {
        // Act
        var result = _controller.GetAssignment(42);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        Assert.NotNull(ok.Value);

        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);

        // Placeholder returns assignment with the requested ID
        var dataJson = GetRawProperty(ok.Value, "data");
        Assert.NotNull(dataJson);
        var assignment = JsonSerializer.Deserialize<JsonElement>(dataJson!);
        Assert.Equal(42, GetInt32Property(assignment, "assignmentId"));
    }

    [Fact]
    public void GetAssignment_ResponseContainsRequiredFields()
    {
        // Act
        var result = _controller.GetAssignment(1);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var dataJson = GetRawProperty(ok.Value, "data");
        Assert.NotNull(dataJson);

        var assignment = JsonSerializer.Deserialize<JsonElement>(dataJson!);
        Assert.True(TryGetCaseInsensitive(assignment, "assignmentid", out _));
        Assert.True(TryGetCaseInsensitive(assignment, "status", out var status));
        Assert.NotEmpty(status.GetString()!);
        Assert.True(TryGetCaseInsensitive(assignment, "assignedby", out _));
    }

    // ---------- GET /api/assignments/delivery/{deliveryId} ----------

    [Fact]
    public void GetAssignmentByDelivery_ReturnsAssignmentWithMatchingDeliveryId()
    {
        // Act
        var result = _controller.GetAssignmentByDelivery(7);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);

        var dataJson = GetRawProperty(ok.Value, "data");
        Assert.NotNull(dataJson);
        var assignment = JsonSerializer.Deserialize<JsonElement>(dataJson!);
        Assert.Equal(7, GetInt32Property(assignment, "deliveryId"));
    }

    // ---------- POST /api/assignments ----------

    [Fact]
    public void CreateAssignment_ValidRequest_Returns201()
    {
        // Arrange
        var controller = CreateControllerWithHttpContext();
        var request = new CreateAssignmentRequest
        {
            DeliveryId = 10,
            VehicleId = 2,
            DriverId = 3,
            Notes = "Handle with care"
        };

        // Act
        var result = controller.CreateAssignment(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);

        var success = GetPropertyValue<bool>(objectResult.Value, "success");
        Assert.True(success);

        var dataJson = GetRawProperty(objectResult.Value, "data");
        Assert.NotNull(dataJson);
        var assignment = JsonSerializer.Deserialize<JsonElement>(dataJson!);
        // Placeholder always sets AssignmentId = 1 and Status = "Active"
        Assert.Equal(1, GetInt32Property(assignment, "assignmentId"));
        Assert.Equal("Active", GetStringProperty(assignment, "status"));
    }

    [Fact]
    public void CreateAssignment_RequestFieldsMappedToAssignment()
    {
        // Arrange
        var controller = CreateControllerWithHttpContext();
        var request = new CreateAssignmentRequest
        {
            DeliveryId = 5,
            VehicleId = 8,
            DriverId = 12
        };

        // Act
        var result = controller.CreateAssignment(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, objectResult.StatusCode);

        var dataJson = GetRawProperty(objectResult.Value, "data");
        Assert.NotNull(dataJson);
        var assignment = JsonSerializer.Deserialize<JsonElement>(dataJson!);
        Assert.Equal(5, GetInt32Property(assignment, "deliveryId"));
        Assert.Equal(8, GetInt32Property(assignment, "vehicleId"));
        Assert.Equal(12, GetInt32Property(assignment, "driverId"));
    }

    // ---------- PUT /api/assignments/{id}/complete ----------

    [Fact]
    public void CompleteAssignment_Returns200WithSuccessMessage()
    {
        // Act
        var result = _controller.CompleteAssignment(5);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        Assert.NotNull(ok.Value);

        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);

        var message = GetPropertyValue<string>(ok.Value, "message");
        Assert.NotNull(message);
        Assert.Contains("completed", message, StringComparison.OrdinalIgnoreCase);
    }

    // ---------- PUT /api/assignments/{id}/cancel ----------

    [Fact]
    public void CancelAssignment_Returns200WithSuccessMessage()
    {
        // Act
        var result = _controller.CancelAssignment(5);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        Assert.NotNull(ok.Value);

        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);

        var message = GetPropertyValue<string>(ok.Value, "message");
        Assert.NotNull(message);
        Assert.Contains("cancel", message, StringComparison.OrdinalIgnoreCase);
    }

    // ---------- GET /api/assignments/driver/{driverId}/active ----------

    [Fact]
    public void GetActiveAssignmentByDriver_ReturnsAssignmentWithMatchingDriverId()
    {
        // Act
        var result = _controller.GetActiveAssignmentByDriver(9);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);

        var dataJson = GetRawProperty(ok.Value, "data");
        Assert.NotNull(dataJson);
        var assignment = JsonSerializer.Deserialize<JsonElement>(dataJson!);
        Assert.Equal(9, GetInt32Property(assignment, "driverId"));
        Assert.Equal("Active", GetStringProperty(assignment, "status"));
    }

    // ---------- Helpers ----------

    /// <summary>
    /// Creates a controller with a fake HttpContext so User (ClaimsPrincipal) is available.
    /// Required for CreateAssignment which calls User.FindFirst(ClaimTypes.NameIdentifier).
    /// </summary>
    private static AssignmentsController CreateControllerWithHttpContext()
    {
        var controller = new AssignmentsController(null!, null!, null!);
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "test-dispatcher-id")
        ], "TestAuth"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
        return controller;
    }

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
        throw new KeyNotFoundException($"Property '{propertyName}' not found.");
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
