using EquilibraFitPlusPlus.Api.Controllers;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EquilibraFitPlusPlus.Api.IntegrationTests.Controllers;

/// <summary>
/// Tests shared HTTP result mappings.
/// </summary>
public sealed class ApiControllerBaseTests
{
    /// <summary>
    /// Ensures persistence conflicts are exposed as HTTP 409 for sync clients.
    /// </summary>
    [Theory]
    [InlineData("treinos.posicao_concorrente")]
    [InlineData("sync_conflict")]
    public void HandleResult_ShouldMapPersistenceConflictToHttp409(string errorCode)
    {
        var controller = new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        Result<object> result = Result<object>.Failure(
            new Error(
                errorCode,
                "Os dados foram alterados em outra operação."));

        IActionResult actionResult = controller.Handle(result);

        var conflict = Assert.IsType<ConflictObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    /// <summary>Provider failures must not be mistaken for invalid credentials or validation errors.</summary>
    [Theory]
    [InlineData("auth.provider_timeout", StatusCodes.Status504GatewayTimeout)]
    [InlineData("auth.provider_unavailable", StatusCodes.Status503ServiceUnavailable)]
    public void HandleResult_ShouldMapAuthProviderFailures(string code, int status)
    {
        var controller = new TestController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var response = Assert.IsType<ObjectResult>(controller.Handle(Result<object>.Failure(new Error(code, "Fixture-safe-message"))));

        Assert.Equal(status, response.StatusCode);
    }

    private sealed class TestController : ApiControllerBase
    {
        public IActionResult Handle(Result<object> result) => HandleResult(result);
    }
}
