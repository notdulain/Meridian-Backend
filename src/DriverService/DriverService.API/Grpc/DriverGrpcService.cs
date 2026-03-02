using Grpc.Core;
using Meridian.DriverGrpc;

namespace DriverService.API.Grpc;

public class DriverGrpcService : DriverGrpc.DriverGrpcBase
{
    private readonly ILogger<DriverGrpcService> _logger;

    public DriverGrpcService(ILogger<DriverGrpcService> logger)
    {
        _logger = logger;
    }

    public override Task<DriverResponse> GetDriver(DriverRequest request, ServerCallContext context)
    {
        // Placeholder
        return Task.FromResult(new DriverResponse
        {
             DriverId = request.DriverId,
             KeycloakUserId = "sub",
             FullName = "John Doe",
             LicenseNumber = "XYZ",
             LicenseExpiry = "2025",
             PhoneNumber = "xxx",
             MaxWorkingHoursPerDay = 8,
             CurrentWorkingHoursToday = 0,
             IsActive = true
        });
    }

    public override Task<AvailabilityResponse> CheckAvailability(DriverRequest request, ServerCallContext context)
    {
        // Placeholder
        return Task.FromResult(new AvailabilityResponse
        {
             IsAvailable = true,
             Message = "Driver is available"
        });
    }

    public override Task<UpdateHoursResponse> UpdateWorkingHours(UpdateHoursRequest request, ServerCallContext context)
    {
        // Placeholder
        return Task.FromResult(new UpdateHoursResponse
        {
             Success = true,
             Message = "Hours updated",
             NewTotalHours = request.HoursToAdd
        });
    }
}
