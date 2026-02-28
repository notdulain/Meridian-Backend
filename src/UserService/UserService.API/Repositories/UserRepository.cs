using UserService.API.Models;

namespace UserService.API.Repositories;

public class UserRepository : IUserRepository
{
    public Task<IEnumerable<User>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public Task<User?> GetByIdAsync(int userId)
    {
        throw new NotImplementedException();
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        throw new NotImplementedException();
    }

    public Task<User> CreateAsync(User user)
    {
        throw new NotImplementedException();
    }

    public Task<User?> UpdateAsync(User user)
    {
        throw new NotImplementedException();
    }

    public Task<bool> SoftDeleteAsync(int userId)
    {
        throw new NotImplementedException();
    }
}
