using RouteService.API.Helpers;
using Xunit;

namespace RouteService.Tests;

public class FuelCostCalculatorTests
{
    public static TheoryData<double, double, double, double, double, double> FuelMetricCases => new()
    {
        { 120000, 15, 350, 120, 8, 2800 },
        { 0, 12, 303, 0, 0, 0 },
        { 100500, 12, 303, 100.5, 8.38, 2537.63 },
        { 1500, 15, 330, 1.5, 0.1, 33 }
    };

    public static TheoryData<double> InvalidDistanceValues => new()
    {
        double.NaN,
        double.PositiveInfinity,
        double.NegativeInfinity
    };

    [Theory]
    [MemberData(nameof(FuelMetricCases))]
    public void CalculateFuelMetrics_ReturnsExpectedRoundedValues_ForSupportedInputs(
        double distanceMeters,
        double fuelEfficiency,
        double fuelPrice,
        double expectedDistanceKm,
        double expectedFuelConsumptionLitres,
        double expectedFuelCostLkr)
    {
        var result = FuelCostCalculator.CalculateFuelMetrics(distanceMeters, fuelEfficiency, fuelPrice);

        Assert.Equal(expectedDistanceKm, result.DistanceKm);
        Assert.Equal(expectedFuelConsumptionLitres, result.FuelConsumptionLitres);
        Assert.Equal(expectedFuelCostLkr, result.FuelCostLKR);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void CalculateFuelMetrics_ThrowsArgumentException_WhenFuelEfficiencyInvalid(double fuelEfficiency)
    {
        Assert.Throws<ArgumentException>(() => FuelCostCalculator.CalculateFuelMetrics(1000, fuelEfficiency, 303));
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void CalculateFuelMetrics_ThrowsArgumentException_WhenFuelPriceInvalid(double fuelPrice)
    {
        Assert.Throws<ArgumentException>(() => FuelCostCalculator.CalculateFuelMetrics(1000, 12, fuelPrice));
    }

    [Theory]
    [MemberData(nameof(InvalidDistanceValues))]
    public void CalculateFuelMetrics_ThrowsArgumentException_WhenDistanceMetersInvalid(double distanceMeters)
    {
        Assert.Throws<ArgumentException>(() => FuelCostCalculator.CalculateFuelMetrics(distanceMeters, 12, 303));
    }
}
