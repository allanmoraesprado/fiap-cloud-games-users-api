using UsersApi.Application.Validators;
using FluentAssertions;
using Xunit;

namespace UsersApi.Tests;

public class UserValidatorTests
{
    [Theory]
    [InlineData("user@fcg.com", true)]
    [InlineData("name.surname@example.co", true)]
    [InlineData("", false)]
    [InlineData("not-an-email", false)]
    [InlineData("missing@domain", false)]
    public void ValidateEmail_works(string email, bool expected)
        => UserValidator.ValidateEmail(email).IsValid.Should().Be(expected);

    [Theory]
    [InlineData("Abcdefg1!", true)]
    [InlineData("Strong@Password1", true)]
    [InlineData("short1!", false)]
    [InlineData("alllowercase1!", true)]
    [InlineData("NoNumber!!", false)]
    [InlineData("NoSpecial123", false)]
    [InlineData("12345678!", false)]
    [InlineData("", false)]
    public void ValidatePassword_enforces_strength(string password, bool expected)
        => UserValidator.ValidatePassword(password).IsValid.Should().Be(expected);

    [Fact]
    public void ValidateRegistration_aggregates_errors()
    {
        var result = UserValidator.ValidateRegistration("", "bad", "weak");
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ValidateRegistration_succeeds_with_valid_data()
    {
        var result = UserValidator.ValidateRegistration("John", "john@fcg.com", "Strong@1pwd");
        result.IsValid.Should().BeTrue();
    }
}
