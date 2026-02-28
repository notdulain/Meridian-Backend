using UserService.API.Models;

namespace UserService.API.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    public Task<RefreshToken> CreateAsync(RefreshToken refreshToken)
    {
        throw new NotImplementedException();
    }

    public Task<RefreshToken?> GetByTokenAsync(string token)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RevokeAsync(string token)
    {
        throw new NotImplementedException();
    }
}
