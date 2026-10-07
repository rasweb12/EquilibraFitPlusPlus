using System.Reflection;
using EquilibraFitPlusPlus.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquilibraFitPlusPlus.Api.IntegrationTests.Controllers;

/// <summary>
/// Tests route contracts for administrative endpoints.
/// </summary>
public sealed class AdminControllerContractTests
{
    /// <summary>
    /// Ensures the administrative controller is versioned and restricted to administrators.
    /// </summary>
    [Fact]
    public void AdminController_ShouldRequireAdminPolicyAndUseVersionedRoute()
    {
        var authorize = typeof(AdminController).GetCustomAttribute<AuthorizeAttribute>();
        var route = typeof(AdminController).GetCustomAttribute<RouteAttribute>();

        Assert.NotNull(authorize);
        Assert.Equal("AdminOnly", authorize!.Policy);
        Assert.NotNull(route);
        Assert.Equal("api/v1/admin", route!.Template);
    }
}
