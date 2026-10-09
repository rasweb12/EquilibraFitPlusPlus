using System.Security.Claims;
using EquilibraFitPlusPlus.Application.Abstractions.AiCoach;
using EquilibraFitPlusPlus.Contracts.Common;
using EquilibraFitPlusPlus.Shared.Results;
using Microsoft.AspNetCore.Mvc;

namespace EquilibraFitPlusPlus.Api.Controllers;

/// <summary>
/// Base controller for API responses.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Converts an application result into an HTTP response.
    /// </summary>
    protected IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return HandleErrors(result);
    }

    /// <summary>
    /// Converts an application result without body into an HTTP response.
    /// </summary>
    protected IActionResult HandleResult(Result result)
    {
        if (result.IsSuccess)
        {
            return NoContent();
        }

        return HandleErrors(result);
    }

    /// <summary>
    /// Extracts the authenticated user and tenant identifiers from JWT claims.
    /// </summary>
    protected bool TryGetUserContext(out Guid tenantId, out Guid usuarioId)
    {
        usuarioId = Guid.Empty;
        tenantId = Guid.Empty;

        string? usuarioIdClaim = User.FindFirstValue("usuario_id");
        string? tenantIdClaim = User.FindFirstValue("tenant_id");

        return Guid.TryParse(usuarioIdClaim, out usuarioId)
            && Guid.TryParse(tenantIdClaim, out tenantId);
    }

    /// <summary>
    /// Returns a safe unauthorized response when the token does not contain required application claims.
    /// </summary>
    protected IActionResult UnauthorizedUserContext()
    {
        var response = new ApiErrorResponse(
            HttpContext.TraceIdentifier,
            "auth.invalid_context",
            "Sessão inválida. Entre novamente para continuar.",
            []);

        return Unauthorized(response);
    }

    private IActionResult HandleErrors(Result result)
    {
        string traceId = HttpContext.TraceIdentifier;
        var details = result.Errors
            .Select(error => new ApiFieldError(error.Field, error.Message))
            .ToArray();

        string code = result.Errors.FirstOrDefault()?.Code ?? "request_error";
        string message = result.Errors.FirstOrDefault()?.Message ?? "Não foi possível concluir a solicitação.";
        var response = new ApiErrorResponse(traceId, code, message, details);

        int? aiStatus = code switch
        {
            AiServiceErrors.Timeout => StatusCodes.Status504GatewayTimeout,
            AiServiceErrors.Unavailable or AiServiceErrors.NotConfigured => StatusCodes.Status503ServiceUnavailable,
            AiServiceErrors.BadGateway or AiServiceErrors.Authentication or AiServiceErrors.InvalidResponse => StatusCodes.Status502BadGateway,
            _ => null
        };
        if (aiStatus.HasValue)
        {
            return StatusCode(aiStatus.Value, response);
        }

        if (code is "auth.provider_timeout" or "auth.provider_unavailable")
        {
            return StatusCode(code == "auth.provider_timeout"
                ? StatusCodes.Status504GatewayTimeout
                : StatusCodes.Status503ServiceUnavailable, response);
        }

        if (code is "auth.invalid_credentials" or "auth.invalid_refresh_token")
        {
            return Unauthorized(response);
        }

        if (code.Contains("nao_encontrad", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(response);
        }

        if (code is "treinos.posicao_concorrente" or "sync_conflict")
        {
            return Conflict(response);
        }

        return BadRequest(response);
    }
}
