namespace FitApi.Dtos;

public record RegisterRequest(string Name, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string Password);
public record UserDto(string Id, string Email, string DisplayName, string? PhotoUrl, bool EmailConfirmed);
