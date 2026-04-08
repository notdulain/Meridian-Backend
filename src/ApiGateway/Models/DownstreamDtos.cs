namespace ApiGateway.Models;

public class DeliveryListItem
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime Deadline { get; set; }
}

public class VehicleListItem
{
    public int VehicleId { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class DriverListItem
{
    public int DriverId { get; set; }
    public double MaxWorkingHoursPerDay { get; set; }
    public double CurrentWorkingHoursToday { get; set; }
    public bool IsActive { get; set; }
}

public class AssignmentListItem
{
    public int AssignmentId { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class DownstreamPagedEnvelope<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public DownstreamMeta? Meta { get; set; }
}

public class DownstreamMeta
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
