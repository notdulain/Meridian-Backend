using UserService.API.DTOs;

namespace UserService.API.Services;

public class AuthService : IAuthService
{
    public Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<AuthResponse> RefreshAsync(RefreshRequest request)
    {
        throw new NotImplementedException();
    }

    public Task RevokeAsync(RevokeRequest request)
    {
        throw new NotImplementedException();
    }
}
