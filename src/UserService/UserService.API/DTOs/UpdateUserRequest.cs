namespace UserService.API.DTOs;

public record UpdateUserRequest(
    string FullName,
    string Email
);
