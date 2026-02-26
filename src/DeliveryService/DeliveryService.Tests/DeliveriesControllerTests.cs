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
