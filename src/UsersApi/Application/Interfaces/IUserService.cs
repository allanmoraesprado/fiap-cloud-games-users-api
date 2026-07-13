using UsersApi.Application.Dtos.Auth;

namespace UsersApi.Application.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<UserResponse>> ListAsync(CancellationToken ct = default);
    Task<UserResponse> GetAsync(Guid id, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
