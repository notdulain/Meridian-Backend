using VehicleService.API.Repositories;
using Xunit;

namespace VehicleService.Tests;

public class VehicleUtilizationReportQueryBuilderTests
{
    [Fact]
    public void BuildDeliveredTripsQuery_ReturnsExpectedMetricAliasesAndFilters()
    {
        var sql = VehicleUtilizationReportQueryBuilder.BuildDeliveredTripsQuery();

        Assert.Contains("AssignedVehicleId AS VehicleId", sql);
        Assert.Contains("TripMinutes", sql);
        Assert.Contains("d.Status = 'Delivered'", sql);
        Assert.Contains("@StartDateUtc", sql);
        Assert.Contains("@EndDateUtc", sql);
        Assert.Contains("FROM Deliveries", sql);
    }

    [Fact]
    public void BuildSelectedRouteHistoriesQuery_ReturnsExpectedFilters()
    {
        var sql = VehicleUtilizationReportQueryBuilder.BuildSelectedRouteHistoriesQuery();

        Assert.Contains("Origin", sql);
        Assert.Contains("Destination", sql);
        Assert.Contains("DistanceKm", sql);
        Assert.Contains("CreatedAt", sql);
        Assert.Contains("FROM RouteHistories", sql);
        Assert.Contains("Selected = 1", sql);
    }
}
