using UsersApi.Application.Interfaces;
using UsersApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace UsersApi.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(UsersDbContext context, IPasswordHasher hasher, CancellationToken ct = default)
    {
        await context.Database.MigrateAsync(ct);

        if (!await context.Users.AnyAsync(ct))
        {
            var admin = new User("Administrator", "admin@fcg.com", hasher.Hash("Admin@123"), UserRole.Admin);
            var user = new User("Regular User", "user@fcg.com", hasher.Hash("User@123"), UserRole.User);
            await context.Users.AddRangeAsync(new[] { admin, user }, ct);
            await context.SaveChangesAsync(ct);
        }
    }
}
