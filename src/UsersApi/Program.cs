using UsersApi.Application.Interfaces;
using UsersApi.Configuration;
using UsersApi.Infrastructure.Persistence;
using UsersApi.Middleware;
using Serilog;

// Keeps Npgsql DateTime handling compatible with the current domain model.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddFcgServices(builder.Configuration);
builder.Services.AddFcgAuth(builder.Configuration);
builder.Services.AddFcgSwagger();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Apply migrations and seed baseline users on startup (dev convenience).
using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    try { await DbSeeder.SeedAsync(ctx, hasher); }
    catch (Exception ex) { Log.Warning(ex, "Database seeding failed (database may be unavailable)."); }
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
