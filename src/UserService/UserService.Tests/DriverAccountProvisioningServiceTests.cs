using Moq;
using UserService.API.DTOs;
using UserService.API.Exceptions;
using UserService.API.Models;
using UserService.API.Repositories;
using UserService.API.Services;
using Xunit;

namespace UserService.Tests;

public class DriverAccountProvisioningServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IDriverProvisioningClient> _driverProvisioningClientMock = new();

    [Fact]
    public async Task CreateDriverAccountAsync_CreatesUserAndDriverProfile()
    {
        var service = CreateService();
        var request = CreateRequest();

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);
        _userRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User user) =>
            {
                user.UserId = 42;
                return user;
            });
        _driverProvisioningClientMock
            .Setup(c => c.CreateDriverProfileAsync(request, 42, "Bearer admin-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriverProfileResponse(
                DriverId: 7,
                UserId: "42",
                FullName: request.FullName,
                LicenseNumber: request.LicenseNumber,
                LicenseExpiry: request.LicenseExpiry,
                PhoneNumber: request.PhoneNumber,
                MaxWorkingHoursPerDay: request.MaxWorkingHoursPerDay,
                CurrentWorkingHoursToday: 0,
                IsActive: true,
                CreatedAt: DateTime.UtcNow,
                UpdatedAt: DateTime.UtcNow));

        var result = await service.CreateDriverAccountAsync(request, "Bearer admin-token");

        Assert.Equal(42, result.User.UserId);
        Assert.Equal("Driver", result.User.Role);
        Assert.Equal(7, result.Driver.DriverId);
        _userRepositoryMock.Verify(r => r.SoftDeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CreateDriverAccountAsync_SoftDeletesCreatedUser_WhenDriverProvisioningFails()
    {
        var service = CreateService();
        var request = CreateRequest();

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);
        _userRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User user) =>
            {
                user.UserId = 21;
                return user;
            });
        _driverProvisioningClientMock
            .Setup(c => c.CreateDriverProfileAsync(request, 21, "Bearer admin-token", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("License number already exists."));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateDriverAccountAsync(request, "Bearer admin-token"));

        Assert.Equal("License number already exists.", ex.Message);
        _userRepositoryMock.Verify(r => r.SoftDeleteAsync(21), Times.Once);
    }

    [Fact]
    public async Task CreateDriverAccountAsync_RejectsDuplicateEmail()
    {
        var service = CreateService();
        var request = CreateRequest();

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(request.Email))
            .ReturnsAsync(new User { UserId = 5, Email = request.Email });

        await Assert.ThrowsAsync<ResourceConflictException>(() =>
            service.CreateDriverAccountAsync(request, "Bearer admin-token"));

        _userRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    private DriverAccountProvisioningService CreateService() =>
        new(_userRepositoryMock.Object, _driverProvisioningClientMock.Object);

    private static CreateDriverAccountRequest CreateRequest() =>
        new()
        {
            FullName = "Driver User",
            Email = "driver@example.com",
            Password = "Password123!",
            LicenseNumber = "LIC-12345",
            LicenseExpiry = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd"),
            PhoneNumber = "+94771234567",
            MaxWorkingHoursPerDay = 8
        };
}
