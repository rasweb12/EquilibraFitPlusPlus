using System.Security.Claims;
using EquilibraFitPlusPlus.Infrastructure.Authentication;

namespace EquilibraFitPlusPlus.Infrastructure.IntegrationTests.Authentication;

public sealed class SupabaseSessionPolicyTests
{
    [Theory]
    [InlineData("[]")]
    [InlineData("invalid-json")]
    [InlineData("[{\"method\":\"token_refresh\",\"timestamp\":300}]")]
    [InlineData("[{\"method\":\"password\",\"timestamp\":100}]")]
    public void MissingOrOldAuthentication_ShouldRemainRevokedAfterTokenRefresh(string amr)
    {
        Assert.True(SupabaseSessionPolicy.IsRevoked([new Claim("iat", "300"), new Claim("amr", amr)], DateTimeOffset.FromUnixTimeSeconds(200)));
    }

    [Theory]
    [InlineData("[{\"method\":\"password\",\"timestamp\":300}]")]
    [InlineData("{\"method\":\"password\",\"timestamp\":300}")]
    public void NewPasswordSignIn_ShouldBeAllowedForBothJwtClaimRepresentations(string amr)
    {
        Assert.False(SupabaseSessionPolicy.IsRevoked([new Claim("iat", "300"), new Claim("amr", amr)], DateTimeOffset.FromUnixTimeSeconds(200)));
    }
}
