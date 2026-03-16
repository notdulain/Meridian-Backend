using Grpc.Core;
using Meridian.VehicleGrpc;
using VehicleService.API.Services;

namespace VehicleService.API.Grpc;

public class VehicleGrpcService : VehicleGrpc.VehicleGrpcBase
{
    private readonly IVehicleService _vehicleService;
    private readonly ILogger<VehicleGrpcService> _logger;

    public VehicleGrpcService(IVehicleService vehicleService, ILogger<VehicleGrpcService> logger)
    {
        _vehicleService = vehicleService;
        _logger = logger;
    }

    public override async Task<VehicleResponse> GetVehicle(VehicleRequest request, ServerCallContext context)
    {
        try
        {
            var vehicle = await _vehicleService.GetVehicleByIdAsync(request.VehicleId);
            if (vehicle == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Vehicle with ID {request.VehicleId} not found"));
            }

            return new VehicleResponse
            {
                VehicleId = vehicle.VehicleId,
                PlateNumber = vehicle.PlateNumber,
                Make = vehicle.Make,
                Model = vehicle.Model,
                Year = vehicle.Year,
                CapacityKg = vehicle.CapacityKg,
                CapacityM3 = vehicle.CapacityM3,
                FuelEfficiencyKmPerLitre = vehicle.FuelEfficiencyKmPerLitre,
                Status = vehicle.Status
            };
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            _logger.LogError(ex, "Error getting vehicle {VehicleId}", request.VehicleId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<AvailabilityResponse> CheckAvailability(VehicleRequest request, ServerCallContext context)
    {
        try
        {
            var vehicle = await _vehicleService.GetVehicleByIdAsync(request.VehicleId);
            if (vehicle == null)
            {
                return new AvailabilityResponse { IsAvailable = false, Message = "Vehicle not found" };
            }

            var isAvailable = vehicle.Status == "Available";
            return new AvailabilityResponse
            {
                IsAvailable = isAvailable,
                Message = isAvailable ? "Vehicle is available" : $"Vehicle is currently {vehicle.Status}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking availability for vehicle {VehicleId}", request.VehicleId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<UpdateStatusResponse> UpdateStatus(UpdateStatusRequest request, ServerCallContext context)
    {
        try
        {
            var success = await _vehicleService.UpdateVehicleStatusAsync(request.VehicleId, request.NewStatus);
            if (!success)
            {
                return new UpdateStatusResponse { Success = false, Message = "Vehicle not found or status not updated" };
            }

            return new UpdateStatusResponse { Success = true, Message = "Status updated successfully" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for vehicle {VehicleId}", request.VehicleId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<GetAvailableVehiclesResponse> GetAvailableVehicles(GetAvailableVehiclesRequest request, ServerCallContext context)
    {
        try
        {
            var vehicles = await _vehicleService.GetAvailableVehiclesAsync();
            var response = new GetAvailableVehiclesResponse();

            foreach (var vehicle in vehicles)
            {
                response.Vehicles.Add(new VehicleResponse
                {
                    VehicleId = vehicle.VehicleId,
                    PlateNumber = vehicle.PlateNumber,
                    Make = vehicle.Make,
                    Model = vehicle.Model,
                    Year = vehicle.Year,
                    CapacityKg = vehicle.CapacityKg,
                    CapacityM3 = vehicle.CapacityM3,
                    FuelEfficiencyKmPerLitre = vehicle.FuelEfficiencyKmPerLitre,
                    Status = vehicle.Status
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available vehicles");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }
}
