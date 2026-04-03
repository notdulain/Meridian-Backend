using UserService.API.DTOs;

namespace UserService.API.Services;

public interface IDriverProvisioningClient
{
    Task<DriverProfileResponse> CreateDriverProfileAsync(
        CreateDriverAccountRequest request,
        int userId,
        string authorizationHeader,
        CancellationToken cancellationToken = default);
}
