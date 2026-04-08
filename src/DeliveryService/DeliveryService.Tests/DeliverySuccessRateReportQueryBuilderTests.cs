using DeliveryService.API.Repositories;
using Xunit;

namespace DeliveryService.Tests;

public class DeliverySuccessRateReportQueryBuilderTests
{
    [Fact]
    public void BuildSuccessRateAggregationQuery_IncludesAllRequiredStatusBuckets()
    {
        var query = DeliverySuccessRateReportQueryBuilder.BuildSuccessRateAggregationQuery();

        Assert.Contains("d.Status = 'Delivered'", query);
        Assert.Contains("d.Status = 'Failed'", query);
        Assert.Contains("d.Status = 'Cancelled'", query);
        Assert.Contains("AS DeliveredCount", query);
        Assert.Contains("AS FailedCount", query);
        Assert.Contains("AS CancelledCount", query);
    }

    [Fact]
    public void BuildSuccessRateAggregationQuery_UsesDeliveredOverTerminalFormula()
    {
        var query = DeliverySuccessRateReportQueryBuilder.BuildSuccessRateAggregationQuery();

        Assert.Contains("AS TerminalCount", query);
        Assert.Contains("AS SuccessRatePercentage", query);
        Assert.Contains("100.0 * SUM(CASE WHEN d.Status = 'Delivered' THEN 1 ELSE 0 END)", query);
        Assert.Contains("/ SUM(CASE WHEN d.Status IN ('Delivered', 'Failed', 'Cancelled') THEN 1 ELSE 0 END)", query);
    }

    [Fact]
    public void BuildSuccessRateAggregationQuery_IncludesDateRangeFilters()
    {
        var query = DeliverySuccessRateReportQueryBuilder.BuildSuccessRateAggregationQuery();

        Assert.Contains("@StartDateUtc", query);
        Assert.Contains("@EndDateUtc", query);
        Assert.Contains("d.CreatedAt >= @StartDateUtc", query);
        Assert.Contains("d.CreatedAt < @EndDateUtc", query);
    }
}