namespace UserService.API.DTOs;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn
);
