using VehicleService.API.Repositories;
using Xunit;

namespace VehicleService.Tests;

public class VehicleUtilizationReportQueryBuilderTests
{
    [Fact]
    public void BuildMetricsQuery_Throws_WhenDeliveryDatabaseNameIsMissing()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            VehicleUtilizationReportQueryBuilder.BuildMetricsQuery("", "route_db"));

        Assert.Equal("deliveryDatabaseName", ex.ParamName);
    }

    [Fact]
    public void BuildMetricsQuery_Throws_WhenRouteDatabaseNameIsMissing()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            VehicleUtilizationReportQueryBuilder.BuildMetricsQuery("delivery_db", ""));

        Assert.Equal("routeDatabaseName", ex.ParamName);
    }

    [Fact]
    public void BuildMetricsQuery_Throws_WhenDatabaseNameContainsUnsafeCharacters()
    {
        Assert.Throws<ArgumentException>(() =>
            VehicleUtilizationReportQueryBuilder.BuildMetricsQuery("delivery-db", "route_db"));

        Assert.Throws<ArgumentException>(() =>
            VehicleUtilizationReportQueryBuilder.BuildMetricsQuery("delivery_db", "route db"));
    }

    [Fact]
    public void BuildMetricsQuery_ReturnsExpectedMetricAliasesAndFilters()
    {
        var sql = VehicleUtilizationReportQueryBuilder.BuildMetricsQuery("delivery_db", "route_db");

        Assert.Contains("AS TripsCount", sql);
        Assert.Contains("AS KilometersDriven", sql);
        Assert.Contains("AS IdleTimeMinutes", sql);
        Assert.Contains("d.Status = 'Delivered'", sql);
        Assert.Contains("rh.Selected = 1", sql);
        Assert.Contains("@StartDateUtc", sql);
        Assert.Contains("@EndDateUtc", sql);
        Assert.Contains("FROM [delivery_db].[dbo].[Deliveries]", sql);
        Assert.Contains("FROM [route_db].[dbo].[RouteHistories]", sql);
    }
}