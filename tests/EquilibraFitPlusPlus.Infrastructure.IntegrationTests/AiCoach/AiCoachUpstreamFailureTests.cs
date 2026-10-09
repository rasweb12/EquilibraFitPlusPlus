using System.Net;
using System.Text;
using EquilibraFitPlusPlus.Application.Abstractions.AiCoach;
using EquilibraFitPlusPlus.Infrastructure.AiCoach;
using EquilibraFitPlusPlus.Shared.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EquilibraFitPlusPlus.Infrastructure.IntegrationTests.AiCoach;

/// <summary>All AI operations share a safe, non-retrying transport boundary.</summary>
public sealed class AiCoachUpstreamFailureTests
{
    /// <summary>Each operation must classify HTML errors, auth failures and invalid responses.</summary>
    public static IEnumerable<object[]> Failures()
    {
        foreach (string operation in new[] { "coach", "meal", "text", "label", "plan", "workout" })
        {
            yield return [operation, 502, "text/html", "<html>private-user-and-key</html>", AiServiceErrors.BadGateway];
            yield return [operation, 401, "application/json", "private-user-and-key", AiServiceErrors.Authentication];
            yield return [operation, 503, "text/html", "private-user-and-key", AiServiceErrors.Unavailable];
            yield return [operation, 504, "text/html", "private-user-and-key", AiServiceErrors.Timeout];
            yield return [operation, 200, "application/json", "not JSON private-user-and-key", AiServiceErrors.InvalidResponse];
            yield return [operation, 200, "application/json", "null", AiServiceErrors.InvalidResponse];
            yield return [operation, 200, "text/html", "private-user-and-key", AiServiceErrors.InvalidResponse];
        }
    }

    /// <summary>Transport errors must never include upstream body text or silently resend POSTs.</summary>
    [Theory]
    [MemberData(nameof(Failures))]
    public async Task Failure_ShouldBeSafeAndNeverRetry(string operation, int status, string mediaType, string body, string code)
    {
        var handler = new ResponseHandler(() => new((HttpStatusCode)status) { Content = new StringContent(body, Encoding.UTF8, mediaType) });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://ai.example.test") };
        var logger = new CapturingLogger();
        var client = new AiCoachHttpClient(http, Options.Create(new AiCoachOptions()), logger);
        Result result = await Invoke(client, operation);
        Assert.Equal(code, Assert.Single(result.Errors).Code);
        Assert.Equal(1, handler.Calls);
        Assert.DoesNotContain("private-user-and-key", result.Errors.Single().Message);
        Assert.DoesNotContain(logger.Messages, message => message.Contains("private-user-and-key", StringComparison.Ordinal));
    }

    /// <summary>Do not download even a single byte of a failing upstream body.</summary>
    [Fact]
    public async Task HtmlError_ShouldNotBeBuffered()
    {
        var content = new UnreadableErrorContent();
        var handler = new ResponseHandler(() => new(HttpStatusCode.BadGateway) { Content = content });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://ai.example.test") };
        var result = await Invoke(new AiCoachHttpClient(http, Options.Create(new AiCoachOptions()), new CapturingLogger()), "coach");
        Assert.Equal(AiServiceErrors.BadGateway, Assert.Single(result.Errors).Code);
        Assert.False(content.ReadAttempted);
    }

    /// <summary>The deadline includes waiting for headers and reading the response body.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Timeout_ShouldBeDistinctFromClinicalFailure(bool duringBody)
    {
        var handler = new ResponseHandler(() => new(HttpStatusCode.OK) { Content = new SlowContent() }, wait: !duringBody);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://ai.example.test"), Timeout = TimeSpan.FromMilliseconds(30) };
        var result = await Invoke(new AiCoachHttpClient(http, Options.Create(new AiCoachOptions()), new CapturingLogger()), "coach");
        Assert.Equal(AiServiceErrors.Timeout, Assert.Single(result.Errors).Code);
        Assert.Equal(1, handler.Calls);
    }

    /// <summary>Caller cancellation is not an upstream timeout.</summary>
    [Fact]
    public async Task CallerCancellation_ShouldPropagate()
    {
        var handler = new ResponseHandler(() => new(HttpStatusCode.OK), wait: true);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://ai.example.test") };
        using var ct = new CancellationTokenSource(30);
        var client = new AiCoachHttpClient(http, Options.Create(new AiCoachOptions()), new CapturingLogger());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Invoke(client, "coach", ct.Token));
    }

    /// <summary>Connection errors must not leak exception details or be retried.</summary>
    [Fact]
    public async Task ConnectionFailure_ShouldReturnUnavailable()
    {
        var handler = new ResponseHandler(() => throw new HttpRequestException("private-user-and-key"));
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://ai.example.test") };
        var logger = new CapturingLogger();
        var result = await Invoke(new AiCoachHttpClient(http, Options.Create(new AiCoachOptions()), logger), "coach");
        Assert.Equal(AiServiceErrors.Unavailable, Assert.Single(result.Errors).Code);
        Assert.Equal(1, handler.Calls);
        Assert.DoesNotContain(logger.Messages, message => message.Contains("private-user-and-key", StringComparison.Ordinal));
    }

    private static async Task<Result> Invoke(AiCoachHttpClient client, string operation, CancellationToken ct = default) => operation switch
    {
        "coach" => await client.EnviarAsync(new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "test", "v1", "{}"), ct),
        "meal" => await client.ReconhecerRefeicaoAsync(new("image", null), ct),
        "text" => await client.EstimarRefeicaoTextoAsync(new("test", null), ct),
        "label" => await client.ReconhecerRotuloAsync(new("image", null), ct),
        "plan" => await client.GerarPlanoAlimentarAsync(new("test", null, [], [], 1800, 2200, null), ct),
        "workout" => await client.GerarTreinoAsync(new("test", "iniciante", 3, [], []), ct),
        _ => throw new InvalidOperationException()
    };

    private sealed class ResponseHandler(Func<HttpResponseMessage> response, bool wait = false) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            if (wait) await Task.Delay(System.Threading.Timeout.Infinite, ct);
            return response();
        }
    }

    private sealed class UnreadableErrorContent : HttpContent
    {
        public bool ReadAttempted { get; private set; }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            ReadAttempted = true;
            throw new InvalidOperationException("An error body must never be downloaded.");
        }
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
    }

    private sealed class SlowContent : HttpContent
    {
        public SlowContent() => Headers.ContentType = new("application/json");
        protected override Task<Stream> CreateContentReadStreamAsync() => Task.FromResult<Stream>(new SlowReadStream());
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => throw new InvalidOperationException("Use the content stream.");
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
    }

    private sealed class SlowReadStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            await Task.Delay(200, ct);
            return 0;
        }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class CapturingLogger : ILogger<AiCoachHttpClient>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Assert.Null(exception);
            Messages.Add(formatter(state, exception));
        }
    }
}
