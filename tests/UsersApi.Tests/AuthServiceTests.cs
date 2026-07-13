using UsersApi.Application.Dtos.Auth;
using UsersApi.Application.Interfaces;
using UsersApi.Application.Services;
using UsersApi.Domain;
using UsersApi.Domain.Exceptions;
using UsersApi.Messaging;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace UsersApi.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenService> _jwt = new();
    private readonly Mock<IEventPublisher> _publisher = new();
    private readonly IOptions<KafkaSettings> _kafka = Options.Create(new KafkaSettings());

    private AuthService Build() =>
        new(_users.Object, _uow.Object, _hasher.Object, _jwt.Object, _publisher.Object, _kafka, NullLogger<AuthService>.Instance);

    [Fact]
    public async Task Register_throws_when_email_already_exists()
    {
        _users.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var act = () => Build().RegisterAsync(new RegisterUserRequest("John", "john@fcg.com", "Strong@1pwd"));
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Register_throws_for_invalid_data()
    {
        var act = () => Build().RegisterAsync(new RegisterUserRequest("", "bad", "weak"));
        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Register_succeeds_and_publishes_event()
    {
        _users.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");

        var resp = await Build().RegisterAsync(new RegisterUserRequest("John", "john@fcg.com", "Strong@1pwd"));
        resp.Email.Should().Be("john@fcg.com");
        _users.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _publisher.Verify(p => p.PublishAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_throws_when_user_not_found()
    {
        _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var act = () => Build().LoginAsync(new LoginRequest("missing@fcg.com", "whatever"));
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Login_throws_when_password_invalid()
    {
        var user = new User("John", "john@fcg.com", "hashed", UserRole.User);
        _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var act = () => Build().LoginAsync(new LoginRequest("john@fcg.com", "wrong"));
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Login_succeeds_and_returns_token()
    {
        var user = new User("John", "john@fcg.com", "hashed", UserRole.User);
        _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _jwt.Setup(j => j.Generate(user)).Returns(("token-123", DateTime.UtcNow.AddHours(1)));

        var resp = await Build().LoginAsync(new LoginRequest("john@fcg.com", "Strong@1pwd"));
        resp.Token.Should().Be("token-123");
        resp.Role.Should().Be("User");
    }
}
