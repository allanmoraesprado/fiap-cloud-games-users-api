using UsersApi.Application.Interfaces;
using UsersApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace UsersApi.Infrastructure.Persistence;

public class UserRepository : IUserRepository
{
    private readonly UsersDbContext _db;
    public UserRepository(UsersDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => _db.Users.FirstOrDefaultAsync(x => x.Email == email, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
        => _db.Users.AnyAsync(x => x.Email == email, ct);

    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default)
        => await _db.Users.AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
        => await _db.Users.AddAsync(user, ct);

    public Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Update(user);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Remove(user);
        return Task.CompletedTask;
    }
}
