namespace UserService.API.Services;

public interface IAuthService
{
    Task<object> RegisterAsync(object request);
    Task<object> LoginAsync(object request);
    Task<object> RefreshAsync(object request);
    Task<bool> RevokeAsync(string token);
}
