using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UsersApi.Infrastructure.Persistence;

// Design-time factory so `dotnet ef migrations` does not need to boot the web host.
public class UsersDbContextFactory : IDesignTimeDbContextFactory<UsersDbContext>
{
    public UsersDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
                   ?? "Host=localhost;Port=5432;Database=fcg_users;Username=fcg;Password=fcg";

        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseNpgsql(conn)
            .Options;

        return new UsersDbContext(options);
    }
}
