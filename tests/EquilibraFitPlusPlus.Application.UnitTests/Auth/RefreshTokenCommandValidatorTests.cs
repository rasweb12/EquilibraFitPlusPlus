using EquilibraFitPlusPlus.Application.Features.Auth.Commands.RefreshToken;
using EquilibraFitPlusPlus.Contracts.Auth;

namespace EquilibraFitPlusPlus.Application.UnitTests.Auth;

/// <summary>Refresh tokens are opaque credentials validated by the provider.</summary>
public sealed class RefreshTokenCommandValidatorTests
{
    [Theory]
    [InlineData("fixture-short-token")]
    [InlineData("x")]
    public void Validate_ShouldAcceptNonemptyOpaqueToken(string token)
    {
        var result = Validate(token);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldRejectMissingToken(string? token)
    {
        var result = Validate(token!);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Request.RefreshToken");
    }

    [Fact]
    public void Validate_ShouldAcceptTokenAtSizeLimit()
    {
        Assert.True(Validate(new string('x', 4096)).IsValid);
    }

    [Fact]
    public void Validate_ShouldRejectOversizedToken()
    {
        Assert.False(Validate(new string('x', 4097)).IsValid);
    }

    private static FluentValidation.Results.ValidationResult Validate(string token) =>
        new RefreshTokenCommandValidator().Validate(
            new RefreshTokenCommand(new RefreshTokenRequest(token), "127.0.0.1"));
}
