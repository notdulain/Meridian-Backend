using UserService.API.Models;

namespace UserService.API.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken> CreateAsync(RefreshToken refreshToken);
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<bool> RevokeAsync(string token);
}
