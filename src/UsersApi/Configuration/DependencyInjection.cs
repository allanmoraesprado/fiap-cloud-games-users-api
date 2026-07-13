using System.Text;
using UsersApi.Application.Interfaces;
using UsersApi.Application.Services;
using UsersApi.Infrastructure.Persistence;
using UsersApi.Infrastructure.Security;
using UsersApi.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace UsersApi.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddFcgServices(this IServiceCollection services, IConfiguration config)
    {
        var pgConn = config.GetConnectionString("Postgres")
                     ?? throw new InvalidOperationException("Missing Postgres connection string.");

        services.AddDbContext<UsersDbContext>(opts => opts.UseNpgsql(pgConn));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UsersDbContext>());

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        services.Configure<JwtSettings>(config.GetSection("Jwt"));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.Configure<KafkaSettings>(config.GetSection("Kafka"));
        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

        return services;
    }

    public static IServiceCollection AddFcgAuth(this IServiceCollection services, IConfiguration config)
    {
        var jwt = config.GetSection("Jwt").Get<JwtSettings>()
                  ?? throw new InvalidOperationException("Missing Jwt settings.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
            options.AddPolicy("AuthenticatedUser", p => p.RequireAuthenticatedUser());
        });

        return services;
    }
}
