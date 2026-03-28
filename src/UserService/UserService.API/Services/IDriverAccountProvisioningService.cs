using UserService.API.DTOs;

namespace UserService.API.Services;

public interface IDriverAccountProvisioningService
{
    Task<CreateDriverAccountResponse> CreateDriverAccountAsync(
        CreateDriverAccountRequest request,
        string authorizationHeader,
        CancellationToken cancellationToken = default);
}
