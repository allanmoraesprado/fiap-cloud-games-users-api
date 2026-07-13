namespace UsersApi.Application.Dtos.Auth;

public record UserResponse(Guid Id, string Name, string Email, string Role, DateTime CreatedAt);
