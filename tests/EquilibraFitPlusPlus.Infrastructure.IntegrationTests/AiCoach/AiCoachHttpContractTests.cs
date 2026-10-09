using System.Net;
using System.Text;
using System.Text.Json;
using EquilibraFitPlusPlus.Application.Abstractions.AiCoach;
using EquilibraFitPlusPlus.Infrastructure.AiCoach;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EquilibraFitPlusPlus.Infrastructure.IntegrationTests.AiCoach;

public sealed class AiCoachHttpContractTests
{
    [Fact]
    public async Task LabelContext_ShouldNotBeSerializedAsExtractedText()
    {
        var handler = new FakeHandler("""{"calories":150,"confidence":90,"requires_user_review":true,"model":"gemini-2.5-flash","message":"review"}""");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://ai.example.test") };
        var client = CreateClient(http);
        var result = await client.ReconhecerRotuloAsync(new("image-placeholder", null, "porcao de 30g"), default);
        Assert.True(result.IsSuccess);
        using var payload = JsonDocument.Parse(handler.Body!);
        Assert.False(payload.RootElement.TryGetProperty("extracted_text", out _));
        Assert.Equal("porcao de 30g", payload.RootElement.GetProperty("label_context").GetString());
        Assert.Equal("gemini-2.5-flash", result.Value!.Model);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"items\":null}")]
    [InlineData("{\"items\":[null]}")]
    [InlineData("not JSON")]
    public async Task MalformedMealResponse_ShouldReturnFailureNotThrow(string body)
    {
        using var http = new HttpClient(new FakeHandler(body)) { BaseAddress = new Uri("https://ai.example.test") };
        var client = CreateClient(http);
        Assert.True((await client.ReconhecerRefeicaoAsync(new("image-placeholder", null), default)).IsFailure);
        Assert.True((await client.EstimarRefeicaoTextoAsync(new("description", null), default)).IsFailure);
    }

    [Fact]
    public async Task ProviderErrorBody_ShouldNeverBeLogged()
    {
        using var http = new HttpClient(new FakeHandler("private-user-text-and-key", HttpStatusCode.UnprocessableEntity))
            { BaseAddress = new Uri("https://ai.example.test") };
        var logger = new CapturingLogger();
        var client = new AiCoachHttpClient(http, Options.Create(new AiCoachOptions()), logger);
        var result = await client.ReconhecerRotuloAsync(new("image-placeholder", null), default);
        Assert.True(result.IsFailure);
        Assert.NotEmpty(logger.Messages);
        Assert.DoesNotContain(logger.Messages, message => message.Contains("private-user-text-and-key"));
    }

    private static AiCoachHttpClient CreateClient(HttpClient http) =>
        new(http, Options.Create(new AiCoachOptions()), NullLogger<AiCoachHttpClient>.Instance);

    private sealed class FakeHandler(string response, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public string? Body { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Body = await request.Content!.ReadAsStringAsync(ct);
            return new(status) { Content = new StringContent(response, Encoding.UTF8, "application/json") };
        }
    }

    private sealed class CapturingLogger : ILogger<AiCoachHttpClient>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
    }
}
