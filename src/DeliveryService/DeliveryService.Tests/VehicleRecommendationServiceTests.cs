using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using Grpc.Core;
using Meridian.VehicleGrpc;
using DeliveryService.API.Repositories;
using DeliveryService.API.Services;
using DeliveryService.API.Models;

namespace DeliveryService.Tests;

public class VehicleRecommendationServiceTests
{
    private readonly Mock<DeliveryRepository> _mockRepo;
    private readonly Mock<VehicleGrpc.VehicleGrpcClient> _mockVehicleClient;
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
        _mockLogger = new Mock<ILogger<VehicleRecommendationService>>();
    }

    [Fact]
    public async Task GetRecommendedVehiclesAsync_ReturnsEmpty_IfDeliveryNotFound()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Delivery?)null);

        var service = new VehicleRecommendationService(_mockRepo.Object, _mockVehicleClient.Object, _mockLogger.Object);

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
            PackageWeightKg = 500,
            PackageVolumeM3 = 5
        };

        var vehiclesFromApi = new List<VehicleResponse>
        {
            new VehicleResponse { VehicleId = 1, Status = "Available", CapacityKg = 400, CapacityM3 = 6, FuelEfficiencyKmPerLitre = 10 },   // Weight too low
            new VehicleResponse { VehicleId = 2, Status = "Available", CapacityKg = 600, CapacityM3 = 4, FuelEfficiencyKmPerLitre = 10 },   // Volume too low
            new VehicleResponse { VehicleId = 3, Status = "OnTrip", CapacityKg = 600, CapacityM3 = 6, FuelEfficiencyKmPerLitre = 10 },      // Not available
            new VehicleResponse { VehicleId = 4, Status = "Available", CapacityKg = 1000, CapacityM3 = 10, FuelEfficiencyKmPerLitre = 5 },  // Valid, inefficient
            new VehicleResponse { VehicleId = 5, Status = "Available", CapacityKg = 600, CapacityM3 = 6, FuelEfficiencyKmPerLitre = 15 }    // Valid, highly efficient
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

        var service = new VehicleRecommendationService(_mockRepo.Object, _mockVehicleClient.Object, _mockLogger.Object);

        // Act
        var result = (await service.GetRecommendedVehiclesAsync(1)).ToList();

        // Assert
        Assert.Equal(2, result.Count); // Only VehicleId 4 and 5 met the criteria

        // Vehicle 5 should be ranked higher due to much better efficiency score and tighter utilization constraints
        Assert.Equal(5, result[0].VehicleId);
        Assert.Equal(4, result[1].VehicleId);
    }
}
