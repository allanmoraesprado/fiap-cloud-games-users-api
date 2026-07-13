using UsersApi.Domain;

namespace UsersApi.Application.Interfaces;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) Generate(User user);
}
