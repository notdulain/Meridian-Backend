using DeliveryService.API.Models;
using DeliveryService.API.Services;
using Xunit;

namespace DeliveryService.Tests;

public class DeliverySuccessRateCalculatorTests
{
    [Fact]
    public void Calculate_ReturnsExpectedBreakdownAndPercentage()
    {
        // delivered=60, failed=30, cancelled=10 => 60 / 100 * 100 = 60%
        var result = DeliverySuccessRateCalculator.Calculate(60, 30, 10);

        Assert.Equal(60, result.DeliveredCount);
        Assert.Equal(30, result.FailedCount);
        Assert.Equal(10, result.CancelledCount);
        Assert.Equal(100, result.TerminalCount);
        Assert.Equal(60m, result.SuccessRatePercentage);
    }

    [Fact]
    public void Calculate_ReturnsZero_WhenNoTerminalDeliveries()
    {
        var result = DeliverySuccessRateCalculator.Calculate(0, 0, 0);

        Assert.Equal(0, result.TerminalCount);
        Assert.Equal(0m, result.SuccessRatePercentage);
    }

    [Fact]
    public void CalculateFromDeliveries_CountsOnlyDeliveredFailedCancelled()
    {
        var deliveries = new List<Delivery>
        {
            new() { Status = "Delivered" },
            new() { Status = "delivered" },
            new() { Status = "Failed" },
            new() { Status = "Cancelled" },
            new() { Status = "Pending" },
            new() { Status = "InTransit" }
        };

        var result = DeliverySuccessRateCalculator.CalculateFromDeliveries(deliveries);

        Assert.Equal(2, result.DeliveredCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal(1, result.CancelledCount);
        Assert.Equal(4, result.TerminalCount);
        Assert.Equal(50m, result.SuccessRatePercentage);
    }

    [Fact]
    public void Calculate_ThrowsForNegativeCounts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DeliverySuccessRateCalculator.Calculate(-1, 0, 0));
    }
}