using System.Net;
using System.Threading.RateLimiting;
using EquilibraFitPlusPlus.Api.Authentication;
using Microsoft.AspNetCore.Http;

namespace EquilibraFitPlusPlus.Api.IntegrationTests;

public sealed class AuthRateLimitPolicyTests
{
    [Fact]
    public void ExhaustingOneAddress_ShouldNotBlockAnotherAddress()
    {
        var policy = new AuthRateLimitPolicy();
        using var limiter = PartitionedRateLimiter.Create<HttpContext, string>(policy.GetPartition);
        var a = new DefaultHttpContext();
        a.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.1");
        var b = new DefaultHttpContext();
        b.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.2");
        for (var index = 0; index < 10; index++)
        {
            using var lease = limiter.AttemptAcquire(a);
            Assert.True(lease.IsAcquired);
        }
        using var blocked = limiter.AttemptAcquire(a);
        using var allowed = limiter.AttemptAcquire(b);
        Assert.False(blocked.IsAcquired);
        Assert.True(allowed.IsAcquired);
    }

    [Fact]
    public void UntrustedForwardedHeader_ShouldNotChoosePartition()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.1");
        var policy = new AuthRateLimitPolicy();
        string original = policy.GetPartition(context).PartitionKey;
        context.Request.Headers["X-Forwarded-For"] = "192.0.2.2";
        Assert.Equal(original, policy.GetPartition(context).PartitionKey);
    }
}
