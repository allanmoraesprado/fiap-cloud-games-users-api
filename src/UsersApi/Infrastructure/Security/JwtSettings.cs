namespace UsersApi.Infrastructure.Security;

public class JwtSettings
{
    public string Issuer { get; set; } = "FiapCloudGames";
    public string Audience { get; set; } = "FiapCloudGames";
    public string SecretKey { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 120;
}
