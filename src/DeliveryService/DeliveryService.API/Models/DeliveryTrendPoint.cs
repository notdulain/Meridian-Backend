namespace DeliveryService.API.Models;

/// <summary>
/// Represents aggregated delivery counts for a single time period bucket
/// (one day, one week, or one month depending on the requested range).
/// </summary>
public class DeliveryTrendPoint
{
    /// <summary>
    /// Human-readable label for the period.
    /// Daily   → "2026-03-01"
    /// Weekly  → "2026-W12"
    /// Monthly → "2026-03"
    /// </summary>
    public string Period { get; init; } = string.Empty;

    public int Total     { get; init; }
    public int Pending   { get; init; }
    public int Assigned  { get; init; }
    public int InTransit { get; init; }
    public int Delivered { get; init; }
    public int Failed    { get; init; }
    public int Canceled  { get; init; }
}
