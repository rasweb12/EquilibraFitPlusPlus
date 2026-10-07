using EquilibraFitPlusPlus.Domain.Entities;

namespace EquilibraFitPlusPlus.Domain.UnitTests.Entities;

/// <summary>
/// Tests refresh token lifecycle rules.
/// </summary>
public sealed class RefreshTokenTests
{
    /// <summary>
    /// Ensures a non-expired and non-revoked refresh token remains active.
    /// </summary>
    [Fact]
    public void Ativo_ShouldBeTrue_WhenTokenIsNotExpiredOrRevoked()
    {
        var token = new RefreshToken
        {
            ExpiraEm = DateTimeOffset.UtcNow.AddMinutes(5)
        };

        Assert.True(token.Ativo);
    }

    /// <summary>
    /// Ensures revoked refresh tokens cannot be used.
    /// </summary>
    [Fact]
    public void Ativo_ShouldBeFalse_WhenTokenIsRevoked()
    {
        var token = new RefreshToken
        {
            ExpiraEm = DateTimeOffset.UtcNow.AddMinutes(5),
            RevogadoEm = DateTimeOffset.UtcNow
        };

        Assert.False(token.Ativo);
    }

    /// <summary>
    /// Ensures expired refresh tokens cannot be used.
    /// </summary>
    [Fact]
    public void Ativo_ShouldBeFalse_WhenTokenIsExpired()
    {
        var token = new RefreshToken
        {
            ExpiraEm = DateTimeOffset.UtcNow.AddMinutes(-1)
        };

        Assert.False(token.Ativo);
    }
}
