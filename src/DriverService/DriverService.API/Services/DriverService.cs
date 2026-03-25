using DriverService.API.Models;
using DriverService.API.Repositories;
using System.Text.RegularExpressions;

namespace DriverService.API.Services;

public class DriverService : IDriverService
{
    private static readonly Regex PhoneNumberPattern = new(@"^\+?[0-9()\-\s]{7,20}$", RegexOptions.Compiled);
    private readonly IDriverRepository _repository;

    public DriverService(IDriverRepository repository)
    {
        _repository = repository;
    }

    public async Task<Driver> CreateDriverAsync(Driver driver)
    {
        ValidateDriver(driver, allowDefaultMaxWorkingHours: true);

        var existing = await _repository.GetByLicenseNumberAsync(driver.LicenseNumber);
        if (existing != null)
            throw new ArgumentException($"A driver with license number '{driver.LicenseNumber}' already exists.");

        return await _repository.CreateAsync(driver);
    }

    public Task<(IEnumerable<Driver> Drivers, int TotalCount)> GetDriversAsync(int page, int pageSize)
        => _repository.GetAllAsync(page, pageSize);

    public Task<(IEnumerable<Driver> Drivers, int TotalCount)> GetDeletedDriversAsync(int page, int pageSize)
        => _repository.GetDeletedAsync(page, pageSize);

    public Task<Driver?> GetDriverByIdAsync(int id)
        => _repository.GetByIdAsync(id);

    public async Task<Driver> UpdateDriverAsync(int id, Driver driver)
    {
        ValidateDriver(driver, allowDefaultMaxWorkingHours: false);

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

    private static void ValidateDriver(Driver driver, bool allowDefaultMaxWorkingHours)
    {
        if (string.IsNullOrWhiteSpace(driver.UserId))
            throw new ArgumentException("User ID is required.");

        driver.UserId = driver.UserId.Trim();
        if (driver.UserId.Length > 100)
            throw new ArgumentException("User ID cannot exceed 100 characters.");

        if (string.IsNullOrWhiteSpace(driver.LicenseNumber))
            throw new ArgumentException("License number is required.");

        driver.LicenseNumber = driver.LicenseNumber.Trim();
        if (driver.LicenseNumber.Length > 50)
            throw new ArgumentException("License number cannot exceed 50 characters.");

        if (string.IsNullOrWhiteSpace(driver.FullName))
            throw new ArgumentException("Full name is required.");

        driver.FullName = driver.FullName.Trim();
        if (driver.FullName.Length > 200)
            throw new ArgumentException("Full name cannot exceed 200 characters.");

        if (string.IsNullOrWhiteSpace(driver.PhoneNumber))
            throw new ArgumentException("Phone number is required.");

        driver.PhoneNumber = driver.PhoneNumber.Trim();
        if (driver.PhoneNumber.Length > 20)
            throw new ArgumentException("Phone number cannot exceed 20 characters.");

        if (!PhoneNumberPattern.IsMatch(driver.PhoneNumber))
            throw new ArgumentException("Phone number must be 7 to 20 characters and contain only digits, spaces, '+', '-' or parentheses.");

        if (!DateTime.TryParse(driver.LicenseExpiry, out var expiryDate))
            throw new ArgumentException("License expiry must be a valid date (e.g. 2027-12-31).");

        if (expiryDate <= DateTime.UtcNow)
            throw new ArgumentException("License expiry date must be in the future.");

        if (driver.MaxWorkingHoursPerDay <= 0)
        {
            if (allowDefaultMaxWorkingHours)
                driver.MaxWorkingHoursPerDay = 8.0; // default if not provided
            else
                throw new ArgumentException("Max working hours per day must be greater than zero.");
        }

        if (driver.MaxWorkingHoursPerDay > 24)
            throw new ArgumentException("Max working hours per day cannot exceed 24.");
    }
}
