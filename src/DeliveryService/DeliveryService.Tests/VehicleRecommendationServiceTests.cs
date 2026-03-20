using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Grpc.Core;
using Meridian.VehicleGrpc;
using DeliveryService.API.Repositories;
using DeliveryService.API.Services;
using DeliveryService.API.Models;
using Microsoft.Extensions.Configuration;

namespace DeliveryService.Tests;

public class VehicleRecommendationServiceTests
{
    private readonly Mock<DeliveryRepository> _mockRepo;
    private readonly Mock<VehicleGrpc.VehicleGrpcClient> _mockVehicleClient;
    private readonly Mock<IRouteDistanceService> _mockRouteDistanceService;
    private readonly Mock<ILogger<VehicleRecommendationService>> _mockLogger;

    public VehicleRecommendationServiceTests()
    {
        // Standard dictionary based IConfiguration setup for both connection string and service URL
        var configItems = new Dictionary<string, string?>
        {
            {"ConnectionStrings:DeliveryDb", "Server=dummy;Database=dummy"},
            {"ServiceUrls:VehicleService", "http://dummy-vehicle-service"}
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(configItems).Build();

        _mockRepo = new Mock<DeliveryRepository>(configuration);
        _mockVehicleClient = new Mock<VehicleGrpc.VehicleGrpcClient>();
        _mockRouteDistanceService = new Mock<IRouteDistanceService>();
        _mockLogger = new Mock<ILogger<VehicleRecommendationService>>();
    }

    [Fact]
    public async Task GetRecommendedVehiclesAsync_ReturnsEmpty_IfDeliveryNotFound()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Delivery?)null);

        var service = new VehicleRecommendationService(_mockRepo.Object, _mockVehicleClient.Object, _mockRouteDistanceService.Object, _mockLogger.Object);

        // Act
        var result = await service.GetRecommendedVehiclesAsync(1);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRecommendedVehiclesAsync_FiltersAndRanksCorrectly()
    {
        // Arrange
        var deliveryInfo = new Delivery
        {
            Id = 1,
            PickupAddress = "Colombo Fort",
            PackageWeightKg = 500,
            PackageVolumeM3 = 5
        };

        var vehiclesFromApi = new List<VehicleResponse>
        {
            new VehicleResponse { VehicleId = 1, Status = "Available", CapacityKg = 400, CapacityM3 = 6, FuelEfficiencyKmPerLitre = 10, CurrentLocation = "Negombo" },
            new VehicleResponse { VehicleId = 2, Status = "Available", CapacityKg = 600, CapacityM3 = 4, FuelEfficiencyKmPerLitre = 10, CurrentLocation = "Moratuwa" },
            new VehicleResponse { VehicleId = 3, Status = "OnTrip", CapacityKg = 600, CapacityM3 = 6, FuelEfficiencyKmPerLitre = 10, CurrentLocation = "Ja-Ela" },
            new VehicleResponse { VehicleId = 4, Status = "Available", CapacityKg = 1000, CapacityM3 = 10, FuelEfficiencyKmPerLitre = 5, CurrentLocation = "Kandy" },
            new VehicleResponse { VehicleId = 5, Status = "Available", CapacityKg = 600, CapacityM3 = 6, FuelEfficiencyKmPerLitre = 15, CurrentLocation = "Colombo 07" }
        };

        var mockResponse = new GetAvailableVehiclesResponse();
        mockResponse.Vehicles.AddRange(vehiclesFromApi);

        var call = new AsyncUnaryCall<GetAvailableVehiclesResponse>(
            Task.FromResult(mockResponse),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { }
        );

        _mockVehicleClient
            .Setup(c => c.GetAvailableVehiclesAsync(It.IsAny<GetAvailableVehiclesRequest>(), null, null, It.IsAny<CancellationToken>()))
            .Returns(call);

        _mockRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(deliveryInfo);
        _mockRouteDistanceService.Setup(s => s.GetDistanceInKilometersAsync("Kandy", "Colombo Fort", It.IsAny<CancellationToken>()))
            .ReturnsAsync(115);
        _mockRouteDistanceService.Setup(s => s.GetDistanceInKilometersAsync("Colombo 07", "Colombo Fort", It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);

        var service = new VehicleRecommendationService(_mockRepo.Object, _mockVehicleClient.Object, _mockRouteDistanceService.Object, _mockLogger.Object);

        // Act
        var result = (await service.GetRecommendedVehiclesAsync(1)).ToList();

        // Assert
        Assert.Equal(2, result.Count); // Only VehicleId 4 and 5 met the criteria

        // Vehicle 5 should rank higher because it is both more efficient and much closer to pickup.
        Assert.Equal(5, result[0].VehicleId);
        Assert.Equal(4, result[1].VehicleId);
        Assert.Equal(4, result[0].DistanceToPickupKm);
        Assert.Equal(115, result[1].DistanceToPickupKm);
    }

    [Fact]
    public async Task GetRecommendedVehiclesAsync_ReturnsEmpty_WhenNoVehiclesAreEligible()
    {
        var deliveryInfo = new Delivery
        {
            Id = 9,
            PickupAddress = "Colombo Fort",
            PackageWeightKg = 500,
            PackageVolumeM3 = 5
        };

        var mockResponse = new GetAvailableVehiclesResponse();
        mockResponse.Vehicles.AddRange(
        [
            new VehicleResponse { VehicleId = 1, Status = "OnTrip", CapacityKg = 700, CapacityM3 = 7, FuelEfficiencyKmPerLitre = 12, CurrentLocation = "Colombo 03" },
            new VehicleResponse { VehicleId = 2, Status = "Available", CapacityKg = 300, CapacityM3 = 3, FuelEfficiencyKmPerLitre = 14, CurrentLocation = "Colombo 05" }
        ]);

        _mockVehicleClient
            .Setup(c => c.GetAvailableVehiclesAsync(It.IsAny<GetAvailableVehiclesRequest>(), null, null, It.IsAny<CancellationToken>()))
            .Returns(CreateAvailableVehiclesCall(Task.FromResult(mockResponse)));
        _mockRepo.Setup(r => r.GetByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(deliveryInfo);

        var service = new VehicleRecommendationService(_mockRepo.Object, _mockVehicleClient.Object, _mockRouteDistanceService.Object, _mockLogger.Object);

        var result = await service.GetRecommendedVehiclesAsync(9);

        Assert.Empty(result);
        _mockRouteDistanceService.Verify(
            s => s.GetDistanceInKilometersAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetRecommendedVehiclesAsync_WhenVehicleServiceRpcFails_ThrowsInvalidOperationException()
    {
        var deliveryInfo = new Delivery
        {
            Id = 3,
            PickupAddress = "Colombo Fort",
            PackageWeightKg = 100,
            PackageVolumeM3 = 1
        };

        var rpcException = new RpcException(new Status(StatusCode.Unavailable, "Vehicle service unavailable"));

        _mockRepo.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(deliveryInfo);
        _mockVehicleClient
            .Setup(c => c.GetAvailableVehiclesAsync(It.IsAny<GetAvailableVehiclesRequest>(), null, null, It.IsAny<CancellationToken>()))
            .Returns(CreateAvailableVehiclesCall(Task.FromException<GetAvailableVehiclesResponse>(rpcException)));

        var service = new VehicleRecommendationService(_mockRepo.Object, _mockVehicleClient.Object, _mockRouteDistanceService.Object, _mockLogger.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetRecommendedVehiclesAsync(3));

        Assert.Contains("VehicleService is unavailable for recommendations", exception.Message);
    }

    [Fact]
    public async Task GetRecommendedVehiclesAsync_WhenVehicleServiceThrowsUnexpectedException_ThrowsInvalidOperationException()
    {
        var deliveryInfo = new Delivery
        {
            Id = 4,
            PickupAddress = "Colombo Fort",
            PackageWeightKg = 100,
            PackageVolumeM3 = 1
        };

        _mockRepo.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(deliveryInfo);
        _mockVehicleClient
            .Setup(c => c.GetAvailableVehiclesAsync(It.IsAny<GetAvailableVehiclesRequest>(), null, null, It.IsAny<CancellationToken>()))
            .Returns(CreateAvailableVehiclesCall(Task.FromException<GetAvailableVehiclesResponse>(new Exception("boom"))));

        var service = new VehicleRecommendationService(_mockRepo.Object, _mockVehicleClient.Object, _mockRouteDistanceService.Object, _mockLogger.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetRecommendedVehiclesAsync(4));

        Assert.Contains("VehicleService is unavailable for recommendations", exception.Message);
    }

    [Fact]
    public async Task GetRecommendedVehiclesAsync_Continues_WhenDistanceLookupFails()
    {
        var deliveryInfo = new Delivery
        {
            Id = 7,
            PickupAddress = "Colombo Fort",
            PackageWeightKg = 100,
            PackageVolumeM3 = 1
        };

        var vehicle = new VehicleResponse
        {
            VehicleId = 11,
            Status = "Available",
            CapacityKg = 600,
            CapacityM3 = 6,
            FuelEfficiencyKmPerLitre = 12,
            CurrentLocation = "Battaramulla"
        };

        var mockResponse = new GetAvailableVehiclesResponse();
        mockResponse.Vehicles.Add(vehicle);

        var call = new AsyncUnaryCall<GetAvailableVehiclesResponse>(
            Task.FromResult(mockResponse),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { }
        );

        _mockVehicleClient
            .Setup(c => c.GetAvailableVehiclesAsync(It.IsAny<GetAvailableVehiclesRequest>(), null, null, It.IsAny<CancellationToken>()))
            .Returns(call);
        _mockRepo.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(deliveryInfo);
        _mockRouteDistanceService
            .Setup(s => s.GetDistanceInKilometersAsync("Battaramulla", "Colombo Fort", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("RouteService unavailable"));

        var service = new VehicleRecommendationService(_mockRepo.Object, _mockVehicleClient.Object, _mockRouteDistanceService.Object, _mockLogger.Object);

        var result = (await service.GetRecommendedVehiclesAsync(7)).Single();

        Assert.Equal(11, result.VehicleId);
        Assert.Null(result.DistanceToPickupKm);
        Assert.Contains("Distance to pickup unavailable", result.RecommendationReason);
    }

    private static AsyncUnaryCall<GetAvailableVehiclesResponse> CreateAvailableVehiclesCall(Task<GetAvailableVehiclesResponse> responseTask)
    {
        return new AsyncUnaryCall<GetAvailableVehiclesResponse>(
            responseTask,
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
    }
}
