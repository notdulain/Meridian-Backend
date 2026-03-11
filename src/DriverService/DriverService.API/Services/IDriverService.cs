using DriverService.API.Models;

namespace DriverService.API.Services;

public interface IDriverService
{
    Task<Driver> CreateDriverAsync(Driver driver);
    Task<(IEnumerable<Driver> Drivers, int TotalCount)> GetDriversAsync(int page, int pageSize);
    Task<(IEnumerable<Driver> Drivers, int TotalCount)> GetDeletedDriversAsync(int page, int pageSize);
    Task<Driver?> GetDriverByIdAsync(int id);
    Task<Driver> UpdateDriverAsync(int id, Driver driver);
    Task<bool> DeleteDriverAsync(int id);
    Task<IEnumerable<Driver>> GetAvailableDriversAsync();
    Task<bool> UpdateWorkingHoursAsync(int id, double hoursToAdd);
}
