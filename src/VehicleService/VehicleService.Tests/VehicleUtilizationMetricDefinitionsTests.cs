using VehicleService.API.Models;
using Xunit;

namespace VehicleService.Tests;

public class VehicleUtilizationMetricDefinitionsTests
{
    [Fact]
    public void VehicleUtilizationMetrics_HasExpectedMetricProperties()
    {
        var metrics = new VehicleUtilizationMetrics
        {
            VehicleId = 12,
            TripsCount = 7,
            KilometersDriven = 154.3,
            IdleTimeMinutes = 315
        };

        Assert.Equal(12, metrics.VehicleId);
        Assert.Equal(7, metrics.TripsCount);
        Assert.Equal(154.3, metrics.KilometersDriven);
        Assert.Equal(315, metrics.IdleTimeMinutes);
    }

    [Fact]
    public void VehicleUtilizationMetricDefinitions_ArePresent()
    {
        Assert.False(string.IsNullOrWhiteSpace(VehicleUtilizationMetricDefinitions.TripsCount));
        Assert.False(string.IsNullOrWhiteSpace(VehicleUtilizationMetricDefinitions.KilometersDriven));
        Assert.False(string.IsNullOrWhiteSpace(VehicleUtilizationMetricDefinitions.IdleTimeMinutes));
        Assert.False(string.IsNullOrWhiteSpace(VehicleUtilizationMetricDefinitions.ActiveTripMinutes));
    }
}