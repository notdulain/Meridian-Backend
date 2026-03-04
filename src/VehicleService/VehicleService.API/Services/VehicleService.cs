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

    public Task<Vehicle> CreateVehicleAsync(Vehicle vehicle)
    {
        return _repository.CreateAsync(vehicle);
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
}
