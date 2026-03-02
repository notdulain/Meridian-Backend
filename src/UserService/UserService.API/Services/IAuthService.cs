using UserService.API.DTOs;

namespace UserService.API.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshAsync(RefreshRequest request);
    Task RevokeAsync(RevokeRequest request);
}
