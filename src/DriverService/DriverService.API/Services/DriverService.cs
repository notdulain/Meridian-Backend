using DriverService.API.Models;
using DriverService.API.Repositories;

namespace DriverService.API.Services;

public class DriverService : IDriverService
{
    private readonly IDriverRepository _repository;

    public DriverService(IDriverRepository repository)
    {
        _repository = repository;
    }

    public async Task<Driver> CreateDriverAsync(Driver driver)
    {
        var existing = await _repository.GetByLicenseNumberAsync(driver.LicenseNumber);
        if (existing != null)
            throw new ArgumentException($"A driver with license number '{driver.LicenseNumber}' already exists.");

        return await _repository.CreateAsync(driver);
    }

    public Task<(IEnumerable<Driver> Drivers, int TotalCount)> GetDriversAsync(int page, int pageSize)
        => _repository.GetAllAsync(page, pageSize);

    public Task<Driver?> GetDriverByIdAsync(int id)
        => _repository.GetByIdAsync(id);

    public async Task<Driver> UpdateDriverAsync(int id, Driver driver)
    {
        // If the license number is changing, ensure the new one doesn't conflict with another driver
        var existingByLicense = await _repository.GetByLicenseNumberAsync(driver.LicenseNumber);
        if (existingByLicense != null && existingByLicense.DriverId != id)
            throw new ArgumentException($"A driver with license number '{driver.LicenseNumber}' already exists.");

        driver.DriverId = id;
        return await _repository.UpdateAsync(driver);
    }

    public Task<bool> DeleteDriverAsync(int id)
        => _repository.DeleteAsync(id);

    public Task<IEnumerable<Driver>> GetAvailableDriversAsync()
        => _repository.GetAvailableAsync();

    public Task<bool> UpdateWorkingHoursAsync(int id, double hoursToAdd)
        => _repository.UpdateWorkingHoursAsync(id, hoursToAdd);
}
