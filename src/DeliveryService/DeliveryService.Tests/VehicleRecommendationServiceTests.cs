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
using DeliveryService.API.Repositories;
using DeliveryService.API.Services;
using DeliveryService.API.Models;

namespace DeliveryService.Tests;

public class VehicleRecommendationServiceTests
{
    private readonly Mock<DeliveryRepository> _mockRepo;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<ILogger<VehicleRecommendationService>> _mockLogger;

    public VehicleRecommendationServiceTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(c => c.GetSection(It.IsAny<string>())).Returns(new Mock<IConfigurationSection>().Object);
        // Setup configuration so that DeliveryRepository doesn't throw
        var mockDbConfigSection = new Mock<IConfigurationSection>();
        mockDbConfigSection.Setup(c => c.Value).Returns("DummyDbPath");
        _mockConfig.Setup(c => c.GetSection("ConnectionStrings")).Returns(mockDbConfigSection.Object);
        var mockDbConfigSectionValue = new Mock<IConfigurationSection>();
        mockDbConfigSectionValue.Setup(c => c.Value).Returns("DummyDbPath");
        _mockConfig.Setup(c => c.GetSection("ConnectionStrings:DeliveryDb")).Returns(mockDbConfigSectionValue.Object);
        // Setup configuration for VehicleRecommendationService
        var mockVehicleServiceUrlSection = new Mock<IConfigurationSection>();
        mockVehicleServiceUrlSection.Setup(s => s.Value).Returns("http://dummy-vehicle-service");
        _mockConfig.Setup(c => c.GetSection("ServiceUrls:VehicleService")).Returns(mockVehicleServiceUrlSection.Object);

        // Standard dictionary based IConfiguration setup for both connection string and service URL
        var configItems = new Dictionary<string, string?>
        {
            {"ConnectionStrings:DeliveryDb", "Server=dummy;Database=dummy"},
            {"ServiceUrls:VehicleService", "http://dummy-vehicle-service"}
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(configItems).Build();


        _mockRepo = new Mock<DeliveryRepository>(configuration);
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockLogger = new Mock<ILogger<VehicleRecommendationService>>();
    }

    [Fact]
    public async Task GetRecommendedVehiclesAsync_ReturnsEmpty_IfDeliveryNotFound()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Delivery?)null);

        var service = new VehicleRecommendationService(_mockRepo.Object, _mockHttpClientFactory.Object, _mockConfig.Object, _mockLogger.Object);

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

        var vehiclesFromApi = new List<object>
        {
            new { VehicleId = 1, Status = "Available", CapacityKg = 400, CapacityM3 = 6, FuelEfficiencyKmPerLitre = 10 },   // Weight too low
            new { VehicleId = 2, Status = "Available", CapacityKg = 600, CapacityM3 = 4, FuelEfficiencyKmPerLitre = 10 },   // Volume too low
            new { VehicleId = 3, Status = "OnTrip", CapacityKg = 600, CapacityM3 = 6, FuelEfficiencyKmPerLitre = 10 },      // Not available
            new { VehicleId = 4, Status = "Available", CapacityKg = 1000, CapacityM3 = 10, FuelEfficiencyKmPerLitre = 5 },  // Valid, inefficient
            new { VehicleId = 5, Status = "Available", CapacityKg = 600, CapacityM3 = 6, FuelEfficiencyKmPerLitre = 15 }    // Valid, highly efficient
        };

        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { success = true, data = vehiclesFromApi })
        };

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>()
           )
           .ReturnsAsync(mockResponse);

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://dummy-vehicle-service") };
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _mockRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(deliveryInfo);

        // Create configuration again inside test with proper service URL to avoid Moq missing mock configuration interface problem
        var configItems = new Dictionary<string, string?>
        {
            {"ConnectionStrings:DeliveryDb", "Server=dummy;Database=dummy"},
            {"ServiceUrls:VehicleService", "http://dummy-vehicle-service"}
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(configItems).Build();

        var service = new VehicleRecommendationService(_mockRepo.Object, _mockHttpClientFactory.Object, config, _mockLogger.Object);

        // Act
        var result = (await service.GetRecommendedVehiclesAsync(1)).ToList();

        // Assert
        Assert.Equal(2, result.Count); // Only VehicleId 4 and 5 met the criteria

        // Vehicle 5 should be ranked higher due to much better efficiency score and tighter utilization constraints
        Assert.Equal(5, result[0].VehicleId);
        Assert.Equal(4, result[1].VehicleId);
    }
}
