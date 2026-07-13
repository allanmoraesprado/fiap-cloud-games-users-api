namespace UsersApi.Application.Dtos.Auth;

public record LoginResponse(string Token, DateTime ExpiresAt, string Email, string Role);
