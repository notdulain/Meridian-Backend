namespace UserService.API.Services;

public class AuthService : IAuthService
{
    public Task<object> RegisterAsync(object request)
    {
        throw new NotImplementedException();
    }

    public Task<object> LoginAsync(object request)
    {
        throw new NotImplementedException();
    }

    public Task<object> RefreshAsync(object request)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RevokeAsync(string token)
    {
        throw new NotImplementedException();
    }
}
