using DeliveryService.API.Controllers;
using DeliveryService.API.DTOs;
using DeliveryService.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DeliveryService.Tests;

public class DeliveriesControllerTests
{
    private readonly Mock<IDeliveryManagerService> _serviceMock;
    private readonly DeliveriesController _controller;

    public DeliveriesControllerTests()
    {
        _serviceMock = new Mock<IDeliveryManagerService>();
        _controller = new DeliveriesController(_serviceMock.Object);
    }

    // ---------- GET /api/deliveries ----------

    [Fact]
    public async Task Get_NoFilters_Returns200WithDefaultPagination()
    {
        // Arrange
        var expectedDeliveries = new List<DeliveryDto>
        {
            new() { Id = 1, Status = "Pending", PickupAddress = "123 Main St", DeliveryAddress = "456 Elm St" },
            new() { Id = 2, Status = "InTransit", PickupAddress = "789 Oak Ave", DeliveryAddress = "321 Pine Rd" }
        };

        _serviceMock
            .Setup(s => s.GetAllDeliveriesAsync(null, null, null, null, 1, 50))
            .ReturnsAsync(expectedDeliveries);

        // Act
        var result = await _controller.Get(null, null, null, null, 1, 50);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var deliveries = Assert.IsAssignableFrom<IEnumerable<DeliveryDto>>(ok.Value);
        Assert.Equal(2, deliveries.Count());
    }

    [Fact]
    public async Task Get_WithStatusFilter_Returns200WithFilteredResults()
    {
        // Arrange
        var expectedDeliveries = new List<DeliveryDto>
        {
            new() { Id = 1, Status = "Delivered", PickupAddress = "123 Main St", DeliveryAddress = "456 Elm St" },
            new() { Id = 3, Status = "Delivered", PickupAddress = "111 Park Ave", DeliveryAddress = "222 Lake Dr" }
        };

        _serviceMock
            .Setup(s => s.GetAllDeliveriesAsync("Delivered", null, null, null, 1, 50))
            .ReturnsAsync(expectedDeliveries);

        // Act
        var result = await _controller.Get("Delivered", null, null, null, 1, 50);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var deliveries = Assert.IsAssignableFrom<IEnumerable<DeliveryDto>>(ok.Value);
        Assert.All(deliveries, d => Assert.Equal("Delivered", d.Status));
    }

    [Fact]
    public async Task Get_WithDestinationFilter_Returns200WithFilteredResults()
    {
        // Arrange
        var expectedDeliveries = new List<DeliveryDto>
        {
            new() { Id = 2, Status = "InTransit", PickupAddress = "789 Oak Ave", DeliveryAddress = "New York" }
        };

        _serviceMock
            .Setup(s => s.GetAllDeliveriesAsync(null, "New York", null, null, 1, 50))
            .ReturnsAsync(expectedDeliveries);

        // Act
        var result = await _controller.Get(null, "New York", null, null, 1, 50);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var deliveries = Assert.IsAssignableFrom<IEnumerable<DeliveryDto>>(ok.Value);
        Assert.Single(deliveries);
        Assert.Equal("New York", deliveries.First().DeliveryAddress);
    }

    [Fact]
    public async Task Get_WithDateFilter_Returns200WithFilteredResults()
    {
        // Arrange
        var filterDate = new DateTime(2026, 2, 27);
        var expectedDeliveries = new List<DeliveryDto>
        {
            new() { Id = 5, Status = "Pending", PickupAddress = "555 Test St", DeliveryAddress = "666 Demo Ave" }
        };

        _serviceMock
            .Setup(s => s.GetAllDeliveriesAsync(null, null, filterDate, null, 1, 50))
            .ReturnsAsync(expectedDeliveries);

        // Act
        var result = await _controller.Get(null, null, filterDate, null, 1, 50);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var deliveries = Assert.IsAssignableFrom<IEnumerable<DeliveryDto>>(ok.Value);
        Assert.Single(deliveries);
    }

    [Fact]
    public async Task Get_WithOrderNumberFilter_Returns200WithFilteredResults()
    {
        // Arrange
        var orderNumber = "ORD-12345";
        var expectedDeliveries = new List<DeliveryDto>
        {
            new() { Id = 7, Status = "Pending", PickupAddress = "100 Commerce Blvd", DeliveryAddress = "200 Business Park" }
        };

        _serviceMock
            .Setup(s => s.GetAllDeliveriesAsync(null, null, null, orderNumber, 1, 50))
            .ReturnsAsync(expectedDeliveries);

        // Act
        var result = await _controller.Get(null, null, null, orderNumber, 1, 50);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var deliveries = Assert.IsAssignableFrom<IEnumerable<DeliveryDto>>(ok.Value);
        Assert.Single(deliveries);
    }

    [Fact]
    public async Task Get_WithMultipleFilters_Returns200WithFilteredResults()
    {
        // Arrange
        var filterDate = new DateTime(2026, 2, 27);
        var expectedDeliveries = new List<DeliveryDto>
        {
            new() { Id = 10, Status = "InTransit", PickupAddress = "300 Main St", DeliveryAddress = "Los Angeles" }
        };

        _serviceMock
            .Setup(s => s.GetAllDeliveriesAsync("InTransit", "Los Angeles", filterDate, null, 1, 50))
            .ReturnsAsync(expectedDeliveries);

        // Act
        var result = await _controller.Get("InTransit", "Los Angeles", filterDate, null, 1, 50);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var deliveries = Assert.IsAssignableFrom<IEnumerable<DeliveryDto>>(ok.Value);
        Assert.Single(deliveries);
        Assert.Equal("InTransit", deliveries.First().Status);
        Assert.Equal("Los Angeles", deliveries.First().DeliveryAddress);
    }

    [Fact]
    public async Task Get_WithCustomPagination_Returns200WithCorrectPageSize()
    {
        // Arrange
        var expectedDeliveries = new List<DeliveryDto>
        {
            new() { Id = 11, Status = "Pending", PickupAddress = "400 Test Ave", DeliveryAddress = "500 Demo St" },
            new() { Id = 12, Status = "Pending", PickupAddress = "600 Sample Rd", DeliveryAddress = "700 Example Ln" },
            new() { Id = 13, Status = "Pending", PickupAddress = "800 Mock Blvd", DeliveryAddress = "900 Fake Dr" }
        };

        _serviceMock
            .Setup(s => s.GetAllDeliveriesAsync(null, null, null, null, 2, 10))
            .ReturnsAsync(expectedDeliveries);

        // Act
        var result = await _controller.Get(null, null, null, null, 2, 10);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var deliveries = Assert.IsAssignableFrom<IEnumerable<DeliveryDto>>(ok.Value);
        Assert.Equal(3, deliveries.Count());
        _serviceMock.Verify(s => s.GetAllDeliveriesAsync(null, null, null, null, 2, 10), Times.Once());
    }

    [Fact]
    public async Task Get_NoMatchingResults_Returns200WithEmptyList()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.GetAllDeliveriesAsync("NonExistentStatus", null, null, null, 1, 50))
            .ReturnsAsync(new List<DeliveryDto>());

        // Act
        var result = await _controller.Get("NonExistentStatus", null, null, null, 1, 50);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var deliveries = Assert.IsAssignableFrom<IEnumerable<DeliveryDto>>(ok.Value);
        Assert.Empty(deliveries);
    }

    // ---------- POST /api/deliveries ----------

    [Fact]
    public async Task Create_ValidRequest_Returns201Created()
    {
        // Arrange
        var request = ValidRequest();
        var expected = new DeliveryDto { Id = 1, Status = "Pending", PickupAddress = request.PickupAddress };
        _serviceMock
            .Setup(s => s.CreateDeliveryAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var created = Assert.IsType<CreatedResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var dto = Assert.IsType<DeliveryDto>(created.Value);
        Assert.Equal(1, dto.Id);
    }

    [Fact]
    public async Task Create_EmptyPickupAddress_Returns400()
    {
        var request = ValidRequest() with { PickupAddress = "" };
        var result = await _controller.Create(request, CancellationToken.None);
        AssertBadRequest(result);
    }

    [Fact]
    public async Task Create_PickupAddressTooLong_Returns400()
    {
        var request = ValidRequest() with { PickupAddress = new string('A', 501) };
        var result = await _controller.Create(request, CancellationToken.None);
        AssertBadRequest(result);
    }

    [Fact]
    public async Task Create_EmptyDeliveryAddress_Returns400()
    {
        var request = ValidRequest() with { DeliveryAddress = "   " };
        var result = await _controller.Create(request, CancellationToken.None);
        AssertBadRequest(result);
    }

    [Fact]
    public async Task Create_ZeroWeight_Returns400()
    {
        var request = ValidRequest() with { PackageWeightKg = 0 };
        var result = await _controller.Create(request, CancellationToken.None);
        AssertBadRequest(result);
    }

    [Fact]
    public async Task Create_NegativeVolume_Returns400()
    {
        var request = ValidRequest() with { PackageVolumeM3 = -1 };
        var result = await _controller.Create(request, CancellationToken.None);
        AssertBadRequest(result);
    }

    [Fact]
    public async Task Create_DeadlineInPast_Returns400()
    {
        var request = ValidRequest() with { Deadline = DateTime.UtcNow.AddDays(-1) };
        var result = await _controller.Create(request, CancellationToken.None);
        AssertBadRequest(result);
    }

    [Fact]
    public async Task Create_EmptyCreatedBy_Returns400()
    {
        var request = ValidRequest() with { CreatedBy = "" };
        var result = await _controller.Create(request, CancellationToken.None);
        AssertBadRequest(result);
    }

    // ---------- GET /api/deliveries/{id} ----------

    [Fact]
    public async Task GetById_ExistingId_Returns200WithDto()
    {
        // Arrange
        var dto = new DeliveryDto { Id = 5, Status = "Pending" };
        _serviceMock
            .Setup(s => s.GetDeliveryByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(5, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<DeliveryDto>(ok.Value);
        Assert.Equal(5, returned.Id);
    }

    [Fact]
    public async Task GetById_NonExistingId_Returns404()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.GetDeliveryByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeliveryDto?)null);

        // Act
        var result = await _controller.GetById(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ---------- Helpers ----------

    private static CreateDeliveryRequestDto ValidRequest() => new()
    {
        PickupAddress = "123 Main St",
        DeliveryAddress = "456 Elm St",
        PackageWeightKg = 2.5m,
        PackageVolumeM3 = 0.1m,
        Deadline = DateTime.UtcNow.AddDays(2),
        CreatedBy = "user-1"
    };

    private static void AssertBadRequest(ActionResult<DeliveryDto> result)
    {
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
