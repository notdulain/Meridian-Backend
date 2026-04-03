using DriverService.API.Models;

namespace DriverService.API.Repositories;

public interface IDriverRepository
{
    Task<Driver> CreateAsync(Driver driver);
    Task<(IEnumerable<Driver> Drivers, int TotalCount)> GetAllAsync(int page, int pageSize);
    Task<(IEnumerable<Driver> Drivers, int TotalCount)> GetDeletedAsync(int page, int pageSize);
    Task<Driver?> GetByIdAsync(int id);
    Task<Driver?> GetByUserIdAsync(string userId);
    Task<Driver?> GetByLicenseNumberAsync(string licenseNumber);
    Task<Driver> UpdateAsync(Driver driver);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<Driver>> GetAvailableAsync();
    Task<bool> UpdateWorkingHoursAsync(int id, double hoursToAdd);
    Task<IEnumerable<DriverPerformanceMetrics>> GetDriverPerformanceReportAsync(DateTime? startDateUtc, DateTime? endDateUtc);
}
