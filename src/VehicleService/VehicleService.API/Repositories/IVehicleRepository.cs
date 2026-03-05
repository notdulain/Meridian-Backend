using VehicleService.API.Models;

namespace VehicleService.API.Repositories;

public interface IVehicleRepository
{
    Task<Vehicle> CreateAsync(Vehicle vehicle);
    Task<(IEnumerable<Vehicle> Vehicles, int TotalCount)> GetAllAsync(int page, int pageSize, string? status, bool? isActive = null);
    Task<Vehicle?> GetByIdAsync(int id);
    Task<Vehicle?> GetByPlateNumberAsync(string plateNumber);
    Task<Vehicle> UpdateAsync(Vehicle vehicle);
    Task<bool> UpdateStatusAsync(int id, string status);
    Task<bool> DeleteAsync(int id); // soft delete
    Task<IEnumerable<Vehicle>> GetAvailableAsync();
}
