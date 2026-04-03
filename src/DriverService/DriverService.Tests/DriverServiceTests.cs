using Moq;
using DriverService.API.Models;
using DriverService.API.Repositories;
using Xunit;

namespace DriverService.Tests;

public class DriverServiceTests
{
    private readonly Mock<IDriverRepository> _repositoryMock;
    private readonly API.Services.DriverService _service;

    public DriverServiceTests()
    {
        _repositoryMock = new Mock<IDriverRepository>();
        _service = new API.Services.DriverService(_repositoryMock.Object);
    }

    // ---------- Helpers ----------

    private static Driver CreateValidDriver() => new()
    {
        UserId = "user-001",
        FullName = "John Doe",
        LicenseNumber = "LIC-001",
        LicenseExpiry = DateTime.UtcNow.AddYears(2).ToString("yyyy-MM-dd"),
        PhoneNumber = "+94771234567",
        MaxWorkingHoursPerDay = 8.0
    };

    // ---------- CreateDriverAsync ----------

    [Fact]
    public async Task CreateDriverAsync_CallsRepository_WhenValid()
    {
        // Arrange
        var driver = CreateValidDriver();
        var created = CreateValidDriver();
        created.DriverId = 1;

        _repositoryMock
            .Setup(r => r.GetByLicenseNumberAsync(driver.LicenseNumber))
            .ReturnsAsync((Driver?)null);
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Driver>()))
            .ReturnsAsync(created);

        // Act
        var result = await _service.CreateDriverAsync(driver);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.DriverId);
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Driver>()), Times.Once);
    }

    [Fact]
    public async Task CreateDriverAsync_ThrowsArgumentException_WhenLicenseNumberAlreadyExists()
    {
        // Arrange
        var driver = CreateValidDriver();
        _repositoryMock
            .Setup(r => r.GetByLicenseNumberAsync(driver.LicenseNumber))
            .ReturnsAsync(new Driver { DriverId = 99, UserId = "other", FullName = "Jane", LicenseNumber = driver.LicenseNumber, LicenseExpiry = "2027-01-01", PhoneNumber = "xxx" });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateDriverAsync(driver));
        Assert.Contains("already exists", ex.Message);
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Driver>()), Times.Never);
    }

    [Fact]
    public async Task CreateDriverAsync_ThrowsArgumentException_WhenUserIdIsBlank()
    {
        var driver = CreateValidDriver();
        driver.UserId = "   ";

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateDriverAsync(driver));
        Assert.Equal("User ID is required.", ex.Message);
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Driver>()), Times.Never);
    }

    [Fact]
    public async Task CreateDriverAsync_ThrowsArgumentException_WhenFullNameIsBlank()
    {
        var driver = CreateValidDriver();
        driver.FullName = "   ";

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateDriverAsync(driver));
        Assert.Equal("Full name is required.", ex.Message);
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Driver>()), Times.Never);
    }

    [Fact]
    public async Task CreateDriverAsync_ThrowsArgumentException_WhenLicenseNumberIsBlank()
    {
        var driver = CreateValidDriver();
        driver.LicenseNumber = " ";

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateDriverAsync(driver));
        Assert.Equal("License number is required.", ex.Message);
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Driver>()), Times.Never);
    }

    [Fact]
    public async Task CreateDriverAsync_ThrowsArgumentException_WhenPhoneNumberIsBlank()
    {
        var driver = CreateValidDriver();
        driver.PhoneNumber = "";

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateDriverAsync(driver));
        Assert.Equal("Phone number is required.", ex.Message);
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Driver>()), Times.Never);
    }

    [Fact]
    public async Task CreateDriverAsync_ThrowsArgumentException_WhenPhoneNumberFormatIsInvalid()
    {
        var driver = CreateValidDriver();
        driver.PhoneNumber = "invalid-phone";

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateDriverAsync(driver));
        Assert.Equal("Phone number must be 7 to 20 characters and contain only digits, spaces, '+', '-' or parentheses.", ex.Message);
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Driver>()), Times.Never);
    }

    [Fact]
    public async Task CreateDriverAsync_ThrowsArgumentException_WhenLicenseExpiryIsInvalid()
    {
        var driver = CreateValidDriver();
        driver.LicenseExpiry = "not-a-date";

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateDriverAsync(driver));
        Assert.Contains("valid date", ex.Message);
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Driver>()), Times.Never);
    }

    [Fact]
    public async Task CreateDriverAsync_ThrowsArgumentException_WhenLicenseExpiryIsInThePast()
    {
        var driver = CreateValidDriver();
        driver.LicenseExpiry = "2020-01-01";

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateDriverAsync(driver));
        Assert.Contains("future", ex.Message);
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Driver>()), Times.Never);
    }

    [Fact]
    public async Task CreateDriverAsync_ThrowsArgumentException_WhenMaxWorkingHoursExceeds24()
    {
        var driver = CreateValidDriver();
        driver.MaxWorkingHoursPerDay = 25;

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateDriverAsync(driver));
        Assert.Contains("cannot exceed 24", ex.Message);
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Driver>()), Times.Never);
    }

    [Fact]
    public async Task CreateDriverAsync_DefaultsMaxWorkingHours_WhenZeroOrNegative()
    {
        var driver = CreateValidDriver();
        driver.MaxWorkingHoursPerDay = 0;

        var created = CreateValidDriver();
        created.DriverId = 1;

        _repositoryMock
            .Setup(r => r.GetByLicenseNumberAsync(It.IsAny<string>()))
            .ReturnsAsync((Driver?)null);
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Driver>()))
            .ReturnsAsync(created);

        // Act — should not throw; MaxWorkingHoursPerDay defaults to 8
        await _service.CreateDriverAsync(driver);

        Assert.Equal(8.0, driver.MaxWorkingHoursPerDay);
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Driver>()), Times.Once);
    }

    // ---------- UpdateDriverAsync ----------

    [Fact]
    public async Task UpdateDriverAsync_ThrowsArgumentException_WhenPlateNumberBelongsToAnotherDriver()
    {
        var driver = CreateValidDriver();
        driver.LicenseNumber = "TAKEN-999";

        _repositoryMock
            .Setup(r => r.GetByLicenseNumberAsync("TAKEN-999"))
            .ReturnsAsync(new Driver { DriverId = 99, UserId = "other", FullName = "Other", LicenseNumber = "TAKEN-999", LicenseExpiry = "2027-01-01", PhoneNumber = "xxx" });

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateDriverAsync(1, driver));
        Assert.Contains("already exists", ex.Message);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Driver>()), Times.Never);
    }

    [Fact]
    public async Task UpdateDriverAsync_ThrowsArgumentException_WhenUserIdIsBlank()
    {
        var driver = CreateValidDriver();
        driver.UserId = "";

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateDriverAsync(1, driver));
        Assert.Equal("User ID is required.", ex.Message);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Driver>()), Times.Never);
    }

    [Fact]
    public async Task UpdateDriverAsync_ThrowsArgumentException_WhenLicenseNumberIsBlank()
    {
        var driver = CreateValidDriver();
        driver.LicenseNumber = " ";

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateDriverAsync(1, driver));
        Assert.Equal("License number is required.", ex.Message);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Driver>()), Times.Never);
    }

    [Fact]
    public async Task UpdateDriverAsync_ThrowsArgumentException_WhenPhoneNumberFormatIsInvalid()
    {
        var driver = CreateValidDriver();
        driver.PhoneNumber = "invalid-phone";

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateDriverAsync(1, driver));
        Assert.Equal("Phone number must be 7 to 20 characters and contain only digits, spaces, '+', '-' or parentheses.", ex.Message);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Driver>()), Times.Never);
    }

    [Fact]
    public async Task UpdateDriverAsync_ThrowsArgumentException_WhenMaxWorkingHoursIsZeroOrNegative()
    {
        var driver = CreateValidDriver();
        driver.MaxWorkingHoursPerDay = 0;

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateDriverAsync(1, driver));
        Assert.Equal("Max working hours per day must be greater than zero.", ex.Message);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Driver>()), Times.Never);
    }

    [Fact]
    public async Task UpdateDriverAsync_ThrowsArgumentException_WhenPhoneNumberExceedsColumnLength()
    {
        var driver = CreateValidDriver();
        driver.PhoneNumber = new string('1', 21);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateDriverAsync(1, driver));
        Assert.Equal("Phone number cannot exceed 20 characters.", ex.Message);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Driver>()), Times.Never);
    }

    [Fact]
    public async Task UpdateDriverAsync_AllowsSameLicenseNumber_WhenBelongsToSameDriver()
    {
        var driver = CreateValidDriver();
        driver.DriverId = 5;
        driver.LicenseNumber = "SAME-001";

        var updated = CreateValidDriver();
        updated.DriverId = 5;

        _repositoryMock
            .Setup(r => r.GetByLicenseNumberAsync("SAME-001"))
            .ReturnsAsync(new Driver { DriverId = 5, UserId = "u", FullName = "Same Driver", LicenseNumber = "SAME-001", LicenseExpiry = "2027-01-01", PhoneNumber = "xxx" });
        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Driver>()))
            .ReturnsAsync(updated);

        var result = await _service.UpdateDriverAsync(5, driver);

        Assert.NotNull(result);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Driver>()), Times.Once);
    }

    // ---------- UpdateWorkingHoursAsync ----------

    [Fact]
    public async Task UpdateWorkingHoursAsync_ThrowsArgumentException_WhenHoursIsZeroOrNegative()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateWorkingHoursAsync(1, 0));
        Assert.Equal("Hours to add must be greater than zero.", ex.Message);
        _repositoryMock.Verify(r => r.UpdateWorkingHoursAsync(It.IsAny<int>(), It.IsAny<double>()), Times.Never);
    }

    [Fact]
    public async Task UpdateWorkingHoursAsync_CallsRepository_WhenValid()
    {
        _repositoryMock
            .Setup(r => r.UpdateWorkingHoursAsync(1, 2.5))
            .ReturnsAsync(true);

        var result = await _service.UpdateWorkingHoursAsync(1, 2.5);

        Assert.True(result);
        _repositoryMock.Verify(r => r.UpdateWorkingHoursAsync(1, 2.5), Times.Once);
    }

    // ---------- GetDriversAsync ----------

    [Fact]
    public async Task GetDriversAsync_ReturnsPaginatedResults()
    {
        var drivers = new List<Driver> { CreateValidDriver(), CreateValidDriver() };
        _repositoryMock
            .Setup(r => r.GetAllAsync(1, 10))
            .ReturnsAsync((drivers, 2));

        var (result, totalCount) = await _service.GetDriversAsync(1, 10);

        Assert.Equal(2, result.Count());
        Assert.Equal(2, totalCount);
    }

    [Fact]
    public async Task GetDriverByUserIdAsync_ReturnsDriver_WhenUserIdExists()
    {
        var driver = CreateValidDriver();
        driver.DriverId = 7;

        _repositoryMock
            .Setup(r => r.GetByUserIdAsync("user-001"))
            .ReturnsAsync(driver);

        var result = await _service.GetDriverByUserIdAsync("user-001");

        Assert.NotNull(result);
        Assert.Equal(7, result!.DriverId);
    }

    [Fact]
    public async Task GetDriverByUserIdAsync_Throws_WhenUserIdIsBlank()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.GetDriverByUserIdAsync("   "));

        Assert.Equal("User ID is required.", ex.Message);
        _repositoryMock.Verify(r => r.GetByUserIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetDeletedDriversAsync_ReturnsPaginatedResults()
    {
        var drivers = new List<Driver> { CreateValidDriver() };
        _repositoryMock
            .Setup(r => r.GetDeletedAsync(1, 10))
            .ReturnsAsync((drivers, 1));

        var (result, totalCount) = await _service.GetDeletedDriversAsync(1, 10);

        Assert.Single(result);
        Assert.Equal(1, totalCount);
    }

    // ---------- DeleteDriverAsync ----------

    [Fact]
    public async Task DeleteDriverAsync_ReturnsFalse_WhenDriverNotFound()
    {
        _repositoryMock
            .Setup(r => r.DeleteAsync(999))
            .ReturnsAsync(false);

        var result = await _service.DeleteDriverAsync(999);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteDriverAsync_ReturnsTrue_WhenDriverDeleted()
    {
        _repositoryMock
            .Setup(r => r.DeleteAsync(1))
            .ReturnsAsync(true);

        var result = await _service.DeleteDriverAsync(1);

        Assert.True(result);
        _repositoryMock.Verify(r => r.DeleteAsync(1), Times.Once);
    }
}
