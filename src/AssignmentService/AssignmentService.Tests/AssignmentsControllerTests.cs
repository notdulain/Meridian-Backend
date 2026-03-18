using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using AssignmentService.API.Controllers;
using AssignmentService.API.Models;
using AssignmentService.API.Repositories;
using Moq;
using Xunit;
using Meridian.VehicleGrpc;
using Meridian.DriverGrpc;
using Grpc.Core;

namespace AssignmentService.Tests;

public class AssignmentsControllerTests
{
    private readonly Mock<IAssignmentRepository> _repoMock;
    private readonly Mock<VehicleGrpc.VehicleGrpcClient> _vehicleClientMock;
    private readonly Mock<DriverGrpc.DriverGrpcClient> _driverClientMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;

    public AssignmentsControllerTests()
    {
        _repoMock = new Mock<IAssignmentRepository>();
        _vehicleClientMock = new Mock<VehicleGrpc.VehicleGrpcClient>();
        _driverClientMock = new Mock<DriverGrpc.DriverGrpcClient>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
    }

    private AssignmentsController CreateControllerWithHttpContext(string role = "Dispatcher")
    {
        var controller = new AssignmentsController(
            _vehicleClientMock.Object,
            _driverClientMock.Object,
            _httpClientFactoryMock.Object,
            _repoMock.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "test-dispatcher-id"),
            new Claim(ClaimTypes.Role, role)
        ], "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
        return controller;
    }

    private void SetupVehicleAvailable(bool available = true)
    {
        var response = new Meridian.VehicleGrpc.AvailabilityResponse { IsAvailable = available, Message = available ? "Available" : "Vehicle not available" };
        _vehicleClientMock
            .Setup(c => c.CheckAvailabilityAsync(
                It.IsAny<VehicleRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(new AsyncUnaryCall<Meridian.VehicleGrpc.AvailabilityResponse>(
                Task.FromResult(response),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { }));
    }

    private void SetupDriverAvailable(bool available = true)
    {
        var response = new Meridian.DriverGrpc.AvailabilityResponse { IsAvailable = available, Message = available ? "Available" : "Driver not available" };
        _driverClientMock
            .Setup(c => c.CheckAvailabilityAsync(
                It.IsAny<DriverRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(new AsyncUnaryCall<Meridian.DriverGrpc.AvailabilityResponse>(
                Task.FromResult(response),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { }));
    }

    private void SetupVehicleUpdateStatus()
    {
        var response = new UpdateStatusResponse { Success = true, Message = "Updated" };
        _vehicleClientMock
            .Setup(c => c.UpdateStatusAsync(
                It.IsAny<UpdateStatusRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(new AsyncUnaryCall<UpdateStatusResponse>(
                Task.FromResult(response),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { }));
    }

    // ---------- GET /api/assignments ----------

    [Fact]
    public async Task GetAssignments_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.GetAllAsync(1, 10))
            .ReturnsAsync((new List<Assignment>(), 0));

        var result = await CreateControllerWithHttpContext().GetAssignments();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);
    }

    [Fact]
    public async Task GetAssignments_WithCustomPagination_Returns200()
    {
        _repoMock.Setup(r => r.GetAllAsync(2, 5))
            .ReturnsAsync((new List<Assignment>(), 0));

        var result = await CreateControllerWithHttpContext().GetAssignments(page: 2, pageSize: 5);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task GetAssignmentHistory_ReturnsHistoryWithDateFilters()
    {
        var fromDate = new DateTime(2026, 3, 1);
        var toDate = new DateTime(2026, 3, 10);
        _repoMock.Setup(r => r.GetHistoryAsync(fromDate, toDate, 1, 10))
            .ReturnsAsync((new List<AssignmentHistory>
            {
                new()
                {
                    AssignmentHistoryId = 1,
                    AssignmentId = 5,
                    DeliveryId = 8,
                    VehicleId = 2,
                    DriverId = 3,
                    NewStatus = "Completed",
                    Action = "Completed",
                    ChangedBy = "dispatcher-1",
                    ChangedAt = new DateTime(2026, 3, 5)
                }
            }, 1));

        var result = await CreateControllerWithHttpContext().GetAssignmentHistory(fromDate, toDate);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        var success = GetPropertyValue<bool>(ok.Value, "success");
        Assert.True(success);
    }

    [Fact]
    public async Task GetAssignmentHistory_InvalidDateRange_Returns400()
    {
        var result = await CreateControllerWithHttpContext().GetAssignmentHistory(
            new DateTime(2026, 3, 10),
            new DateTime(2026, 3, 1));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    // ---------- GET /api/assignments/{id} ----------

    [Fact]
    public async Task GetAssignment_ReturnsAssignmentById()
    {
        _repoMock.Setup(r => r.GetByIdAsync(42))
            .ReturnsAsync(new Assignment { AssignmentId = 42, DeliveryId = 1, VehicleId = 1, DriverId = 1, AssignedBy = "admin", Status = "Active" });

        var result = await CreateControllerWithHttpContext().GetAssignment(42);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        var dataJson = GetRawProperty(ok.Value, "data");
        var assignment = JsonSerializer.Deserialize<JsonElement>(dataJson!);
        Assert.Equal(42, GetInt32Property(assignment, "assignmentId"));
    }

    [Fact]
    public async Task GetAssignment_NotFound_Returns404()
    {
        _repoMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Assignment?)null);

        var result = await CreateControllerWithHttpContext().GetAssignment(999);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
    }

    [Fact]
    public async Task GetAssignment_ResponseContainsRequiredFields()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Assignment { AssignmentId = 1, DeliveryId = 1, VehicleId = 1, DriverId = 1, AssignedBy = "admin", Status = "Active" });

        var result = await CreateControllerWithHttpContext().GetAssignment(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dataJson = GetRawProperty(ok.Value, "data");
        var assignment = JsonSerializer.Deserialize<JsonElement>(dataJson!);
        Assert.True(TryGetCaseInsensitive(assignment, "assignmentid", out _));
        Assert.True(TryGetCaseInsensitive(assignment, "status", out var status));
        Assert.NotEmpty(status.GetString()!);
        Assert.True(TryGetCaseInsensitive(assignment, "assignedby", out _));
    }

    // ---------- GET /api/assignments/delivery/{deliveryId} ----------

    [Fact]
    public async Task GetAssignmentByDelivery_ReturnsAssignmentWithMatchingDeliveryId()
    {
        _repoMock.Setup(r => r.GetByDeliveryIdAsync(7))
            .ReturnsAsync(new Assignment { AssignmentId = 1, DeliveryId = 7, VehicleId = 1, DriverId = 1, AssignedBy = "admin", Status = "Active" });

        var result = await CreateControllerWithHttpContext().GetAssignmentByDelivery(7);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        var dataJson = GetRawProperty(ok.Value, "data");
        var assignment = JsonSerializer.Deserialize<JsonElement>(dataJson!);
        Assert.Equal(7, GetInt32Property(assignment, "deliveryId"));
    }

    [Fact]
    public async Task GetAssignmentByDelivery_NotFound_Returns404()
    {
        _repoMock.Setup(r => r.GetByDeliveryIdAsync(999))
            .ReturnsAsync((Assignment?)null);

        var result = await CreateControllerWithHttpContext().GetAssignmentByDelivery(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ---------- POST /api/assignments ----------

    [Fact]
    public async Task CreateAssignment_VehicleNotAvailable_Returns409()
    {
        SetupVehicleAvailable(available: false);
        var controller = CreateControllerWithHttpContext();
        var request = new CreateAssignmentRequest { DeliveryId = 1, VehicleId = 2, DriverId = 3 };

        var result = await controller.CreateAssignment(request);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflict.StatusCode);
    }

    [Fact]
    public async Task CreateAssignment_DriverNotAvailable_Returns409()
    {
        SetupVehicleAvailable(available: true);
        SetupDriverAvailable(available: false);
        var controller = CreateControllerWithHttpContext();
        var request = new CreateAssignmentRequest { DeliveryId = 1, VehicleId = 2, DriverId = 3 };

        var result = await controller.CreateAssignment(request);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflict.StatusCode);
    }

    [Fact]
    public async Task CreateAssignment_ValidRequest_PersistsAndReturns201()
    {
        SetupVehicleAvailable(available: true);
        SetupDriverAvailable(available: true);
        SetupVehicleUpdateStatus();

        _httpClientFactoryMock.Setup(f => f.CreateClient("DeliveryService"))
            .Returns(new System.Net.Http.HttpClient { BaseAddress = new Uri("http://localhost:6001") });

        var expectedAssignment = new Assignment
        {
            AssignmentId = 1,
            DeliveryId = 10,
            VehicleId = 2,
            DriverId = 3,
            AssignedBy = "test-dispatcher-id",
            Status = "Active",
            Notes = "Handle with care"
        };
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Assignment>()))
            .ReturnsAsync(expectedAssignment);
        _repoMock.Setup(r => r.CreateHistoryAsync(It.IsAny<AssignmentHistory>()))
            .Returns(Task.CompletedTask);

        var controller = CreateControllerWithHttpContext();
        var request = new CreateAssignmentRequest
        {
            DeliveryId = 10,
            VehicleId = 2,
            DriverId = 3,
            Notes = "Handle with care"
        };

        var result = await controller.CreateAssignment(request);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, objectResult.StatusCode);
        var success = GetPropertyValue<bool>(objectResult.Value, "success");
        Assert.True(success);
        _repoMock.Verify(r => r.CreateHistoryAsync(It.Is<AssignmentHistory>(h =>
            h.AssignmentId == 1 &&
            h.Action == "Created" &&
            h.NewStatus == "Active")), Times.Once);
    }

    // ---------- PUT /api/assignments/{id}/complete ----------

    [Fact]
    public async Task CompleteAssignment_NotFound_Returns404()
    {
        _repoMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Assignment?)null);

        var result = await CreateControllerWithHttpContext().CompleteAssignment(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CompleteAssignment_Returns200WithSuccessMessage()
    {
        var existing = new Assignment { AssignmentId = 5, DeliveryId = 1, VehicleId = 1, DriverId = 1, AssignedBy = "admin", Status = "Active" };
        _repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateStatusAsync(5, "Completed")).ReturnsAsync(true);
        _repoMock.Setup(r => r.CreateHistoryAsync(It.IsAny<AssignmentHistory>())).Returns(Task.CompletedTask);
        SetupVehicleUpdateStatus();
        _httpClientFactoryMock.Setup(f => f.CreateClient("DeliveryService"))
            .Returns(new System.Net.Http.HttpClient { BaseAddress = new Uri("http://localhost:6001") });

        var result = await CreateControllerWithHttpContext().CompleteAssignment(5);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        var message = GetPropertyValue<string>(ok.Value, "message");
        Assert.Contains("completed", message, StringComparison.OrdinalIgnoreCase);
        _repoMock.Verify(r => r.CreateHistoryAsync(It.Is<AssignmentHistory>(h =>
            h.AssignmentId == 5 &&
            h.Action == "Completed" &&
            h.PreviousStatus == "Active" &&
            h.NewStatus == "Completed")), Times.Once);
    }

    // ---------- PUT /api/assignments/{id}/cancel ----------

    [Fact]
    public async Task CancelAssignment_NotFound_Returns404()
    {
        _repoMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Assignment?)null);

        var result = await CreateControllerWithHttpContext().CancelAssignment(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CancelAssignment_Returns200WithSuccessMessage()
    {
        var existing = new Assignment { AssignmentId = 5, DeliveryId = 1, VehicleId = 1, DriverId = 1, AssignedBy = "admin", Status = "Active" };
        _repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateStatusAsync(5, "Cancelled")).ReturnsAsync(true);
        _repoMock.Setup(r => r.CreateHistoryAsync(It.IsAny<AssignmentHistory>())).Returns(Task.CompletedTask);
        SetupVehicleUpdateStatus();
        _httpClientFactoryMock.Setup(f => f.CreateClient("DeliveryService"))
            .Returns(new System.Net.Http.HttpClient { BaseAddress = new Uri("http://localhost:6001") });

        var result = await CreateControllerWithHttpContext().CancelAssignment(5);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        var message = GetPropertyValue<string>(ok.Value, "message");
        Assert.Contains("cancel", message, StringComparison.OrdinalIgnoreCase);
        _repoMock.Verify(r => r.CreateHistoryAsync(It.Is<AssignmentHistory>(h =>
            h.AssignmentId == 5 &&
            h.Action == "Cancelled" &&
            h.PreviousStatus == "Active" &&
            h.NewStatus == "Cancelled")), Times.Once);
    }

    // ---------- GET /api/assignments/driver/{driverId}/active ----------

    [Fact]
    public async Task GetActiveAssignmentByDriver_ReturnsAssignmentWithMatchingDriverId()
    {
        _repoMock.Setup(r => r.GetActiveByDriverIdAsync(9))
            .ReturnsAsync(new Assignment { AssignmentId = 1, DeliveryId = 1, VehicleId = 1, DriverId = 9, AssignedBy = "admin", Status = "Active" });

        var result = await CreateControllerWithHttpContext().GetActiveAssignmentByDriver(9);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        var dataJson = GetRawProperty(ok.Value, "data");
        var assignment = JsonSerializer.Deserialize<JsonElement>(dataJson!);
        Assert.Equal(9, GetInt32Property(assignment, "driverId"));
        Assert.Equal("Active", GetStringProperty(assignment, "status"));
    }

    [Fact]
    public async Task GetActiveAssignmentByDriver_NotFound_Returns404()
    {
        _repoMock.Setup(r => r.GetActiveByDriverIdAsync(99))
            .ReturnsAsync((Assignment?)null);

        var result = await CreateControllerWithHttpContext().GetActiveAssignmentByDriver(99);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ---------- Helpers ----------

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
