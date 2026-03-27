using UserService.API.DTOs;
using UserService.API.Models;
using UserService.API.Repositories;

namespace UserService.API.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<UserResponse>> GetAllAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(MapToResponse);
    }

    public async Task<UserResponse?> GetByIdAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user is null ? null : MapToResponse(user);
    }

    public async Task<UserResponse?> GetMeAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user is null ? null : MapToResponse(user);
    }

    public async Task<UserResponse?> UpdateAsync(int userId, UpdateUserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.UpdatedAt = DateTime.UtcNow;

        var updated = await _userRepository.UpdateAsync(user);
        return updated is null ? null : MapToResponse(updated);
    }

    public async Task<bool> SoftDeleteAsync(int userId)
    {
        return await _userRepository.SoftDeleteAsync(userId);
    }

    private static UserResponse MapToResponse(User user) =>
        new(
            UserId: user.UserId,
            FullName: user.FullName,
            Email: user.Email,
            Role: user.Role.ToString(),
            IsActive: user.IsActive,
            CreatedAt: user.CreatedAt,
            UpdatedAt: user.UpdatedAt);
}
