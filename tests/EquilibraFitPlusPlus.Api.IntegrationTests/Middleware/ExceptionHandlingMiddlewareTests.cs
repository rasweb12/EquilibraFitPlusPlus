using EquilibraFitPlusPlus.Api.Middleware;
using EquilibraFitPlusPlus.Contracts.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace EquilibraFitPlusPlus.Api.IntegrationTests.Middleware;

/// <summary>Client disconnects must not trigger an internal server error body.</summary>
public sealed class ExceptionHandlingMiddlewareTests
{
    /// <summary>Aborted requests end without attempting to send an error body.</summary>
    [Fact]
    public async Task CallerCancellation_ShouldNotWriteInternalServerError()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var context = new DefaultHttpContext { RequestAborted = source.Token };
        using var body = new MemoryStream();
        context.Response.Body = body;
        var middleware = new ExceptionHandlingMiddleware(_ => throw new TaskCanceledException("fixture-caller-cancellation"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status499ClientClosedRequest, context.Response.StatusCode);
        Assert.Equal(0, body.Length);
    }

    /// <summary>Cancellation must not mutate headers after a response has already started.</summary>
    [Fact]
    public async Task CallerCancellation_AfterResponseStarted_ShouldPreserveStatus()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var context = new DefaultHttpContext { RequestAborted = source.Token };
        context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());
        using var body = new MemoryStream();
        context.Response.Body = body;
        var middleware = new ExceptionHandlingMiddleware(_ => throw new TaskCanceledException("fixture-caller-cancellation"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal(0, body.Length);
    }

    /// <summary>An unrelated cancellation remains an unexpected error, not a client disconnect.</summary>
    [Fact]
    public async Task CancellationWithoutAbortedRequest_ShouldRemainInternalServerError()
    {
        var context = new DefaultHttpContext();
        using var body = new MemoryStream();
        context.Response.Body = body;
        var middleware = new ExceptionHandlingMiddleware(_ => throw new TaskCanceledException("fixture-unexpected-cancellation"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        body.Position = 0;
        var error = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal("unexpected_error", error!.Code);
        Assert.DoesNotContain("fixture-unexpected-cancellation", error.Message);
    }

    private sealed class StartedResponseFeature : HttpResponseFeature
    {
        public override bool HasStarted => true;
    }
}
