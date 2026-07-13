using UsersApi.Application.Interfaces;
using UsersApi.Domain;
using UsersApi.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace UsersApi.Infrastructure.Persistence;

public class UsersDbContext : DbContext, IUnitOfWork
{
    public UsersDbContext(DbContextOptions<UsersDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => base.SaveChangesAsync(cancellationToken);
}
