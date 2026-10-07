using System.Reflection;
using EquilibraFitPlusPlus.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EquilibraFitPlusPlus.Api.IntegrationTests.Controllers;

/// <summary>
/// Tests security-sensitive API controller contracts.
/// </summary>
public sealed class SecurityControllerContractTests
{
    /// <summary>
    /// Ensures authentication endpoints are rate limited against brute-force attempts.
    /// </summary>
    [Fact]
    public void AuthController_ShouldUseAuthRateLimitPolicy()
    {
        var rateLimit = typeof(AuthController).GetCustomAttribute<EnableRateLimitingAttribute>();

        Assert.NotNull(rateLimit);
        Assert.Equal("auth", rateLimit!.PolicyName);
    }

    /// <summary>
    /// Ensures LGPD endpoints are authenticated and versioned.
    /// </summary>
    [Fact]
    public void LgpdController_ShouldRequireAuthenticationAndUseVersionedRoute()
    {
        var authorize = typeof(LgpdController).GetCustomAttribute<AuthorizeAttribute>();
        var route = typeof(LgpdController).GetCustomAttribute<RouteAttribute>();

        Assert.NotNull(authorize);
        Assert.NotNull(route);
        Assert.Equal("api/v1/lgpd", route!.Template);
    }

    /// <summary>
    /// Ensures LGPD deletion requests are not exposed through a read endpoint.
    /// </summary>
    [Fact]
    public void LgpdDeletionRequest_ShouldUsePostEndpoint()
    {
        MethodInfo method = typeof(LgpdController).GetMethod(nameof(LgpdController.SolicitarExclusao))
            ?? throw new InvalidOperationException("LGPD deletion endpoint was not found.");

        var httpPost = method.GetCustomAttribute<HttpPostAttribute>();

        Assert.NotNull(httpPost);
        Assert.Equal("solicitar-exclusao", httpPost!.Template);
    }
}
