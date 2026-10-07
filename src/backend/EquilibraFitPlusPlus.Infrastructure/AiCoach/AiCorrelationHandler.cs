using Microsoft.AspNetCore.Http;

namespace EquilibraFitPlusPlus.Infrastructure.AiCoach;

public sealed class AiCorrelationHandler(IHttpContextAccessor context) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (context.HttpContext?.Items["CorrelationId"] is string id)
            request.Headers.TryAddWithoutValidation("X-Correlation-ID", id);
        return base.SendAsync(request, ct);
    }
}
