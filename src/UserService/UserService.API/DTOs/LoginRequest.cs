namespace UserService.API.DTOs;

public record LoginRequest(
    string Email,
    string Password
);
