namespace CreditReporting.Shared.Dtos;

public record LoginRequest(string Username, string Password);

public record LoginResponse(string Token, DateTime ExpiresAtUtc, string Username, string Role);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
