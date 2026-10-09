using EquilibraFitPlusPlus.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using EquilibraFitPlusPlus.Application.Abstractions.Billing;
using EquilibraFitPlusPlus.Application.Abstractions.Data;

namespace EquilibraFitPlusPlus.Api.Middleware;

/// <summary>
/// Converts unhandled exceptions into a safe API response.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Initializes the middleware.
    /// </summary>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Handles the current HTTP request.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug("Request {TraceId} was canceled by the client.", context.TraceIdentifier);
            if (!context.Response.HasStarted) context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
        }
        catch (Exception exception)
        {
            if (exception is BillingValidationException billing)
            {
                context.Response.StatusCode = billing.Code switch
                {
                    "billing.not_configured" or "billing.provider_unavailable" or "billing.acknowledgement_failed" => StatusCodes.Status503ServiceUnavailable,
                    "billing.provider_timeout" => StatusCodes.Status504GatewayTimeout,
                    "billing.provider_invalid_response" => StatusCodes.Status502BadGateway,
                    _ => StatusCodes.Status400BadRequest
                };
                await context.Response.WriteAsJsonAsync(new ApiErrorResponse(context.TraceIdentifier, billing.Code, billing.Message, []));
                return;
            }
            if (exception is PersistenceConflictException persistence)
            {
                context.Response.StatusCode = 409;
                await context.Response.WriteAsJsonAsync(new ApiErrorResponse(context.TraceIdentifier, "sync_conflict", persistence.Message, []));
                return;
            }
            if (exception is DbUpdateConcurrencyException)
            {
                _logger.LogWarning(
                    exception,
                    "Concurrency conflict while processing request {TraceId}.",
                    context.TraceIdentifier);

                context.Response.StatusCode = StatusCodes.Status409Conflict;
                context.Response.ContentType = "application/json";

                var conflictResponse = new ApiErrorResponse(
                    context.TraceIdentifier,
                    "sync_conflict",
                    "Os dados foram alterados em outro dispositivo. Atualize e tente novamente.",
                    []);

                await context.Response.WriteAsJsonAsync(conflictResponse);
                return;
            }

            _logger.LogError(exception, "Unhandled exception while processing request {TraceId}.", context.TraceIdentifier);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new ApiErrorResponse(
                context.TraceIdentifier,
                "unexpected_error",
                "Não foi possível concluir a solicitação. Podemos tentar novamente em instantes.",
                []);

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
