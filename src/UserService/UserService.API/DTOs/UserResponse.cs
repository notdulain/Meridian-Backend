namespace UserService.API.DTOs;

public record UserResponse(
    int UserId,
    string FullName,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
