using UsersApi.Application.Dtos.Auth;
using UsersApi.Application.Interfaces;
using UsersApi.Application.Validators;
using UsersApi.Contracts;
using UsersApi.Domain;
using UsersApi.Domain.Exceptions;
using UsersApi.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace UsersApi.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly IEventPublisher _publisher;
    private readonly KafkaSettings _kafka;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IUnitOfWork uow,
        IPasswordHasher hasher,
        IJwtTokenService jwt,
        IEventPublisher publisher,
        IOptions<KafkaSettings> kafkaOptions,
        ILogger<AuthService> logger)
    {
        _users = users;
        _uow = uow;
        _hasher = hasher;
        _jwt = jwt;
        _publisher = publisher;
        _kafka = kafkaOptions.Value;
        _logger = logger;
    }

    public async Task<UserResponse> RegisterAsync(RegisterUserRequest request, CancellationToken ct = default)
    {
        var validation = UserValidator.ValidateRegistration(request.Name, request.Email, request.Password);
        if (!validation.IsValid)
            throw new DomainException(string.Join(" ", validation.Errors));

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _users.EmailExistsAsync(email, ct))
            throw new ConflictException("Email is already in use.");

        var user = new User(request.Name.Trim(), email, _hasher.Hash(request.Password));
        await _users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("User registered: {Email}", email);

        // Publish AFTER the commit. KafkaEventPublisher swallows/logs broker errors, so a
        // temporarily-unavailable broker never fails a registration that already succeeded.
        var evt = new UserCreatedEvent(Guid.NewGuid(), user.Id, user.Name, user.Email, DateTime.UtcNow);
        await _publisher.PublishAsync(_kafka.UserCreatedTopic, user.Id.ToString(), evt, ct);

        return new UserResponse(user.Id, user.Name, user.Email, user.Role.ToString(), user.CreatedAt);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
            throw new UnauthorizedException("Invalid credentials.");

        var user = await _users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);
        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid credentials.");

        var (token, expires) = _jwt.Generate(user);
        _logger.LogInformation("User logged in: {Email}", user.Email);
        return new LoginResponse(token, expires, user.Email, user.Role.ToString());
    }
}
