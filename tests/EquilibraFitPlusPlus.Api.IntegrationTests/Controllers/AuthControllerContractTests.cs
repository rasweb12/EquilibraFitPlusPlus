using System.Reflection;
using EquilibraFitPlusPlus.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EquilibraFitPlusPlus.Api.IntegrationTests.Controllers;

/// <summary>
/// Tests public authentication endpoint metadata.
/// </summary>
public sealed class AuthControllerContractTests
{
    /// <summary>
    /// Ensures authentication endpoints stay versioned and rate limited.
    /// </summary>
    [Fact]
    public void AuthController_ShouldUseVersionedRouteAndAuthRateLimitPolicy()
    {
        var route = typeof(AuthController).GetCustomAttribute<RouteAttribute>();
        var rateLimit = typeof(AuthController).GetCustomAttribute<EnableRateLimitingAttribute>();

        Assert.NotNull(route);
        Assert.Equal("api/v1/auth", route!.Template);
        Assert.NotNull(rateLimit);
        Assert.Equal("auth", rateLimit!.PolicyName);
    }

    /// <summary>
    /// Ensures the required authentication actions remain available.
    /// </summary>
    [Theory]
    [InlineData(nameof(AuthController.Cadastrar), "cadastrar")]
    [InlineData(nameof(AuthController.Login), "login")]
    [InlineData(nameof(AuthController.Refresh), "refresh")]
    public void AuthActions_ShouldExposeExpectedPostRoutes(string actionName, string template)
    {
        MethodInfo method = typeof(AuthController).GetMethod(actionName)!;
        var post = method.GetCustomAttribute<HttpPostAttribute>();

        Assert.NotNull(post);
        Assert.Equal(template, post!.Template);
    }
}
