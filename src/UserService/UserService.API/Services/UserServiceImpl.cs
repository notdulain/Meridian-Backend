using UserService.API.DTOs;

namespace UserService.API.Services;

public class UserService : IUserService
{
    public Task<IEnumerable<UserResponse>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public Task<UserResponse?> GetByIdAsync(int userId)
    {
        throw new NotImplementedException();
    }

    public Task<UserResponse?> GetMeAsync(int userId)
    {
        throw new NotImplementedException();
    }

    public Task<UserResponse?> UpdateAsync(int userId, UpdateUserRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<bool> SoftDeleteAsync(int userId)
    {
        throw new NotImplementedException();
    }
}
