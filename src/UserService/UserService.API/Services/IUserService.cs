using UserService.API.DTOs;

namespace UserService.API.Services;

public interface IUserService
{
    Task<IEnumerable<UserResponse>> GetAllAsync();
    Task<UserResponse?> GetByIdAsync(int userId);
    Task<UserResponse?> GetMeAsync(int userId);
    Task<UserResponse?> UpdateAsync(int userId, UpdateUserRequest request);
    Task<bool> SoftDeleteAsync(int userId);
}
