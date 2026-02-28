using UserService.API.Models;

namespace UserService.API.Services;

public class UserService : IUserService
{
    public Task<IEnumerable<User>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public Task<User?> GetByIdAsync(int userId)
    {
        throw new NotImplementedException();
    }

    public Task<User?> GetMeAsync(int userId)
    {
        throw new NotImplementedException();
    }

    public Task<User?> UpdateAsync(int userId, object request)
    {
        throw new NotImplementedException();
    }

    public Task<bool> SoftDeleteAsync(int userId)
    {
        throw new NotImplementedException();
    }
}
