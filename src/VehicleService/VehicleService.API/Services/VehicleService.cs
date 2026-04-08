using VehicleService.API.Models;
using VehicleService.API.Repositories;

namespace VehicleService.API.Services;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _repository;

    public VehicleService(IVehicleRepository repository)
    {
        _repository = repository;
    }

    public async Task<Vehicle> CreateVehicleAsync(Vehicle vehicle)
    {
        if (string.IsNullOrWhiteSpace(vehicle.CurrentLocation))
            throw new ArgumentException("Current location is required.");

        if (vehicle.CapacityKg <= 0)
            throw new ArgumentException("Capacity (Kg) must be greater than zero.");
        
        if (vehicle.CapacityM3 <= 0)
            throw new ArgumentException("Capacity (M3) must be greater than zero.");
            
        var existing = await _repository.GetByPlateNumberAsync(vehicle.PlateNumber);
        if (existing != null)
            throw new ArgumentException($"A vehicle with plate number '{vehicle.PlateNumber}' already exists.");

        return await _repository.CreateAsync(vehicle);
    }

    public Task<(IEnumerable<Vehicle> Vehicles, int TotalCount)> GetVehiclesAsync(int page, int pageSize, string? status, bool? isActive = null)
    {
        return _repository.GetAllAsync(page, pageSize, status, isActive);
    }

    public Task<Vehicle?> GetVehicleByIdAsync(int id)
    {
        return _repository.GetByIdAsync(id);
    }

    public async Task<Vehicle> UpdateVehicleAsync(int id, Vehicle vehicle)
    {
        if (string.IsNullOrWhiteSpace(vehicle.CurrentLocation))
            throw new ArgumentException("Current location is required.");

        if (vehicle.CapacityKg <= 0)
            throw new ArgumentException("Capacity (Kg) must be greater than zero.");

        if (vehicle.CapacityM3 <= 0)
            throw new ArgumentException("Capacity (M3) must be greater than zero.");

        var validStatuses = new[] { "Available", "OnTrip", "Maintenance", "Retired" };
        if (!validStatuses.Contains(vehicle.Status))
            throw new ArgumentException($"Invalid status '{vehicle.Status}'. Must be one of: {string.Join(", ", validStatuses)}.");

        var existing = await _repository.GetByPlateNumberAsync(vehicle.PlateNumber);
        if (existing != null && existing.VehicleId != id)
            throw new ArgumentException($"A vehicle with plate number '{vehicle.PlateNumber}' already exists.");

        vehicle.VehicleId = id;
        return await _repository.UpdateAsync(vehicle);
    }

    public Task<bool> UpdateVehicleStatusAsync(int id, string status)
    {
        return _repository.UpdateStatusAsync(id, status);
    }

    public Task<bool> DeleteVehicleAsync(int id)
    {
        return _repository.DeleteAsync(id);
    }

    public Task<IEnumerable<Vehicle>> GetAvailableVehiclesAsync()
    {
        return _repository.GetAvailableAsync();
    }

    public Task<IEnumerable<VehicleUtilizationMetrics>> GetVehicleUtilizationReportAsync(DateTime? startDateUtc, DateTime? endDateUtc)
    {
        if (startDateUtc.HasValue && endDateUtc.HasValue && endDateUtc <= startDateUtc)
            throw new ArgumentException("End date must be greater than start date.");

        return _repository.GetVehicleUtilizationReportAsync(startDateUtc, endDateUtc);
    }
}
