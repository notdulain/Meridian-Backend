using RouteService.API.Helpers;
using Xunit;

namespace RouteService.Tests;

public class FuelCostCalculatorTests
{
    [Fact]
    public void CalculateFuelMetrics_ReturnsExpectedValues_ForValidInput()
    {
        var result = FuelCostCalculator.CalculateFuelMetrics(108000, 12, 303);

        Assert.Equal(108, result.DistanceKm);
        Assert.Equal(9, result.FuelConsumptionLitres);
        Assert.Equal(2727, result.FuelCostLKR);
    }

    [Fact]
    public void CalculateFuelMetrics_ReturnsZero_WhenDistanceIsZero()
    {
        var result = FuelCostCalculator.CalculateFuelMetrics(0, 12, 303);

        Assert.Equal(0, result.DistanceKm);
        Assert.Equal(0, result.FuelConsumptionLitres);
        Assert.Equal(0, result.FuelCostLKR);
    }

    [Fact]
    public void CalculateFuelMetrics_ThrowsArgumentException_WhenFuelEfficiencyInvalid()
    {
        Assert.Throws<ArgumentException>(() => FuelCostCalculator.CalculateFuelMetrics(1000, 0, 303));
        Assert.Throws<ArgumentException>(() => FuelCostCalculator.CalculateFuelMetrics(1000, -1, 303));
    }

    [Fact]
    public void CalculateFuelMetrics_ThrowsArgumentException_WhenFuelPriceNegative()
    {
        Assert.Throws<ArgumentException>(() => FuelCostCalculator.CalculateFuelMetrics(1000, 12, -10));
    }

    [Fact]
    public void CalculateFuelMetrics_RoundsToTwoDecimals()
    {
        var result = FuelCostCalculator.CalculateFuelMetrics(100500, 12, 303);

        Assert.Equal(100.5, result.DistanceKm);
        Assert.Equal(8.38, result.FuelConsumptionLitres);
        Assert.Equal(2537.63, result.FuelCostLKR);
    }
}
