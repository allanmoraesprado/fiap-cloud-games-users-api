using UsersApi.Application.Dtos.Auth;

namespace UsersApi.Application.Interfaces;

public interface IAuthService
{
    Task<UserResponse> RegisterAsync(RegisterUserRequest request, CancellationToken ct = default);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
}
