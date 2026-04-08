using UserService.API.DTOs;
using UserService.API.Exceptions;
using UserService.API.Models;
using UserService.API.Repositories;

namespace UserService.API.Services;

public class DriverAccountProvisioningService : IDriverAccountProvisioningService
{
    private readonly IUserRepository _userRepository;
    private readonly IDriverProvisioningClient _driverProvisioningClient;

    public DriverAccountProvisioningService(
        IUserRepository userRepository,
        IDriverProvisioningClient driverProvisioningClient)
    {
        _userRepository = userRepository;
        _driverProvisioningClient = driverProvisioningClient;
    }

    public async Task<CreateDriverAccountResponse> CreateDriverAccountAsync(
        CreateDriverAccountRequest request,
        string authorizationHeader,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            throw new InvalidOperationException("Authorization header is required to provision a driver account.");
        }

        NormalizeAndValidate(request);

        var existing = await _userRepository.GetByEmailAsync(request.Email);
        if (existing is not null)
        {
            throw new ResourceConflictException("Email is already registered.");
        }

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.Driver,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user = await _userRepository.CreateAsync(user);

        try
        {
            var driver = await _driverProvisioningClient.CreateDriverProfileAsync(
                request,
                user.UserId,
                authorizationHeader,
                cancellationToken);

            return new CreateDriverAccountResponse(MapToResponse(user), driver);
        }
        catch
        {
            await _userRepository.SoftDeleteAsync(user.UserId);
            throw;
        }
    }

    private static void NormalizeAndValidate(CreateDriverAccountRequest request)
    {
        request.FullName = request.FullName.Trim();
        request.Email = request.Email.Trim();
        request.Password = request.Password.Trim();
        request.LicenseNumber = request.LicenseNumber.Trim();
        request.LicenseExpiry = request.LicenseExpiry.Trim();
        request.PhoneNumber = request.PhoneNumber.Trim();

        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ArgumentException("Full name is required.");

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email is required.");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Password is required.");

        if (request.Password.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters.");
    }

    private static UserResponse MapToResponse(User user) =>
        new(
            UserId: user.UserId,
            FullName: user.FullName,
            Email: user.Email,
            Role: user.Role.ToString(),
            IsActive: user.IsActive,
            CreatedAt: user.CreatedAt,
            UpdatedAt: user.UpdatedAt);
}
