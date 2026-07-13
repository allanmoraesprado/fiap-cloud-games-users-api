using UsersApi.Application.Dtos.Auth;
using UsersApi.Application.Interfaces;
using UsersApi.Domain.Exceptions;

namespace UsersApi.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;

    public UserService(IUserRepository users, IUnitOfWork uow)
    {
        _users = users;
        _uow = uow;
    }

    public async Task<IReadOnlyList<UserResponse>> ListAsync(CancellationToken ct = default)
    {
        var list = await _users.ListAsync(ct);
        return list.Select(u => new UserResponse(u.Id, u.Name, u.Email, u.Role.ToString(), u.CreatedAt)).ToList();
    }

    public async Task<UserResponse> GetAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(id, ct) ?? throw new NotFoundException("User not found.");
        return new UserResponse(user.Id, user.Name, user.Email, user.Role.ToString(), user.CreatedAt);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(id, ct) ?? throw new NotFoundException("User not found.");
        await _users.DeleteAsync(user, ct);
        await _uow.SaveChangesAsync(ct);
    }
}
