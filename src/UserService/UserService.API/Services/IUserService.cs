using UserService.API.Models;

namespace UserService.API.Services;

public interface IUserService
{
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?> GetByIdAsync(int userId);
    Task<User?> GetMeAsync(int userId);
    Task<User?> UpdateAsync(int userId, object request);
    Task<bool> SoftDeleteAsync(int userId);
}
