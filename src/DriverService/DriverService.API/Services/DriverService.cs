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
        ValidateDriver(driver);

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
        ValidateDriver(driver);

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
    {
        if (hoursToAdd <= 0)
            throw new ArgumentException("Hours to add must be greater than zero.");

        return _repository.UpdateWorkingHoursAsync(id, hoursToAdd);
    }

    // ---------- Private Helpers ----------

    private static void ValidateDriver(Driver driver)
    {
        if (string.IsNullOrWhiteSpace(driver.FullName))
            throw new ArgumentException("Full name is required.");

        if (string.IsNullOrWhiteSpace(driver.PhoneNumber))
            throw new ArgumentException("Phone number is required.");

        if (!DateTime.TryParse(driver.LicenseExpiry, out var expiryDate))
            throw new ArgumentException("License expiry must be a valid date (e.g. 2027-12-31).");

        if (expiryDate <= DateTime.UtcNow)
            throw new ArgumentException("License expiry date must be in the future.");

        if (driver.MaxWorkingHoursPerDay <= 0)
            driver.MaxWorkingHoursPerDay = 8.0; // default if not provided

        if (driver.MaxWorkingHoursPerDay > 24)
            throw new ArgumentException("Max working hours per day cannot exceed 24.");
    }
}
