using EquilibraFitPlusPlus.Api.Controllers;
using EquilibraFitPlusPlus.Application.Abstractions.AiCoach;
using EquilibraFitPlusPlus.Contracts.Common;
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

    /// <summary>AI transport errors preserve infrastructure status and the safe error envelope.</summary>
    [Theory]
    [InlineData(AiServiceErrors.BadGateway, 502)]
    [InlineData(AiServiceErrors.Authentication, 502)]
    [InlineData(AiServiceErrors.InvalidResponse, 502)]
    [InlineData(AiServiceErrors.Unavailable, 503)]
    [InlineData(AiServiceErrors.NotConfigured, 503)]
    [InlineData(AiServiceErrors.Timeout, 504)]
    public void HandleResult_ShouldMapAiInfrastructureErrors(string code, int status)
    {
        var context = new DefaultHttpContext { TraceIdentifier = "test-request-id" };
        var controller = new TestController { ControllerContext = new() { HttpContext = context } };
        var response = Assert.IsType<ObjectResult>(controller.Handle(Result<object>.Failure(AiServiceErrors.Create(code))));
        Assert.Equal(status, response.StatusCode);
        var body = Assert.IsType<ApiErrorResponse>(response.Value);
        Assert.Equal(code, body.Code);
        Assert.Equal("test-request-id", body.TraceId);
        Assert.DoesNotContain("clinico", body.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Genuine safety blocks retain their business status rather than becoming transport failures.</summary>
    [Theory]
    [InlineData("ia.coach_risco_clinico")]
    [InlineData("ia.coach_linguagem_insegura")]
    public void HandleResult_ShouldKeepClinicalErrorsSeparate(string code)
    {
        var controller = new TestController { ControllerContext = new() { HttpContext = new DefaultHttpContext() } };
        Assert.IsType<BadRequestObjectResult>(controller.Handle(Result<object>.Failure(new Error(code, "Safety block"))));
    }

    private sealed class TestController : ApiControllerBase
    {
        public IActionResult Handle(Result<object> result) => HandleResult(result);
    }
}
