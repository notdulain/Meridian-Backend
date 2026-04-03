using VehicleService.API.Models;

namespace VehicleService.API.Services;

public interface IVehicleService
{
    Task<Vehicle> CreateVehicleAsync(Vehicle vehicle);
    Task<(IEnumerable<Vehicle> Vehicles, int TotalCount)> GetVehiclesAsync(int page, int pageSize, string? status, bool? isActive = null);
    Task<Vehicle?> GetVehicleByIdAsync(int id);
    Task<Vehicle> UpdateVehicleAsync(int id, Vehicle vehicle);
    Task<bool> UpdateVehicleStatusAsync(int id, string status);
    Task<bool> DeleteVehicleAsync(int id);
    Task<IEnumerable<Vehicle>> GetAvailableVehiclesAsync();
    Task<IEnumerable<VehicleUtilizationMetrics>> GetVehicleUtilizationReportAsync(DateTime? startDateUtc, DateTime? endDateUtc);
}
