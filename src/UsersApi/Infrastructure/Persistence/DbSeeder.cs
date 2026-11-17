using UsersApi.Application.Interfaces;
using UsersApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace UsersApi.Infrastructure.Persistence;

// Bound from the "Seed" configuration section. Local defaults live in appsettings.json
// (development placeholders); in the cloud the admin password comes from Key Vault
// (Seed__AdminPassword) and the regular demo user is not seeded (empty UserPassword).
public class SeedSettings
{
    public string AdminEmail { get; set; } = "admin@fcg.com";
    public string? AdminPassword { get; set; }
    public string UserEmail { get; set; } = "user@fcg.com";
    public string? UserPassword { get; set; }
}

public static class DbSeeder
{
    public static async Task SeedAsync(UsersDbContext context, IPasswordHasher hasher, SeedSettings seed, CancellationToken ct = default)
    {
        await context.Database.MigrateAsync(ct);

        if (await context.Users.AnyAsync(ct)) return;

        var users = new List<User>();
        if (!string.IsNullOrWhiteSpace(seed.AdminPassword))
            users.Add(new User("Administrator", seed.AdminEmail, hasher.Hash(seed.AdminPassword), UserRole.Admin));
        if (!string.IsNullOrWhiteSpace(seed.UserPassword))
            users.Add(new User("Regular User", seed.UserEmail, hasher.Hash(seed.UserPassword), UserRole.User));

        if (users.Count == 0) return;
        await context.Users.AddRangeAsync(users, ct);
        await context.SaveChangesAsync(ct);
    }
}
