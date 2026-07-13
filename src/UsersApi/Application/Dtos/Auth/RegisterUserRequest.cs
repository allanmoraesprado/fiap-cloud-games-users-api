namespace UsersApi.Application.Dtos.Auth;

public record RegisterUserRequest(string Name, string Email, string Password);
