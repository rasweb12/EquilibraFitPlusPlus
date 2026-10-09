using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace EquilibraFitPlusPlus.Api.Authentication;

/// <summary>Keep authentication quotas independent for each resolved client address.</summary>
public sealed class AuthRateLimitPolicy : IRateLimiterPolicy<string>
{
    /// <inheritdoc />
    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => null;

    /// <inheritdoc />
    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        // RemoteIpAddress is resolved by the trusted proxy middleware, not a raw request header.
        string key = httpContext.Connection.RemoteIpAddress?.MapToIPv6().ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    }
}
