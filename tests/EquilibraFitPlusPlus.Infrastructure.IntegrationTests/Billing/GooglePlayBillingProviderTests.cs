using System.Net;
using System.Text.Json;
using EquilibraFitPlusPlus.Application.Abstractions.Billing;
using EquilibraFitPlusPlus.Infrastructure.Billing;

namespace EquilibraFitPlusPlus.Infrastructure.IntegrationTests.Billing;

public sealed class GooglePlayBillingProviderTests
{
    [Theory]
    [InlineData(401, "billing.provider_unavailable")]
    [InlineData(403, "billing.provider_unavailable")]
    [InlineData(429, "billing.provider_unavailable")]
    [InlineData(500, "billing.provider_unavailable")]
    [InlineData(503, "billing.provider_unavailable")]
    [InlineData(400, "billing.verification_failed")]
    [InlineData(404, "billing.verification_failed")]
    public async Task VerificationFailure_DistinguishesTemporaryOutage(int status, string code)
    {
        using var http = new HttpClient(new Handler(_ => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)
        { Content = new StringContent("private-provider-fixture") })));
        var error = await Assert.ThrowsAsync<BillingValidationException>(() => Provider(http).VerifyAsync("receipt-fixture", "account", default));
        Assert.Equal(code, error.Code);
        Assert.DoesNotContain("private-provider-fixture", error.ToString());
        Assert.DoesNotContain("receipt-fixture", error.ToString());
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("{\"externalAccountIdentifiers\":{\"obfuscatedExternalAccountId\":\"account\"}}")]
    public async Task MalformedResponse_IsSafeAndRetryable(string body)
    {
        using var http = new HttpClient(new Handler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StringContent(body) })));
        var error = await Assert.ThrowsAsync<BillingValidationException>(() => Provider(http).VerifyAsync("receipt-fixture", "account", default));
        Assert.Equal("billing.provider_invalid_response", error.Code);
        Assert.Null(error.InnerException);
    }

    [Fact]
    public async Task Verification_BindsOwnerAndReadsServerSubscription()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddDays(30);
        var body = JsonSerializer.Serialize(new
        {
            externalAccountIdentifiers = new { obfuscatedExternalAccountId = "account" },
            subscriptionState = "SUBSCRIPTION_STATE_ACTIVE",
            acknowledgementState = "ACKNOWLEDGEMENT_STATE_PENDING",
            startTime = DateTimeOffset.UtcNow.AddDays(-1),
            testPurchase = new { },
            lineItems = new[] { new { productId = "test.monthly", expiryTime = expiresAt,
                autoRenewingPlan = new { autoRenewEnabled = true }, latestSuccessfulOrderId = "test-order" } }
        });
        using var http = new HttpClient(new Handler(request =>
        {
            Assert.Equal("androidpublisher.googleapis.com", request.RequestUri!.Host);
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
        }));
        var provider = Provider(http);
        var result = await provider.VerifyAsync("receipt-fixture", "account", default);
        Assert.Equal("test.monthly", result.ProductId);
        Assert.Equal(expiresAt, result.ExpiresAt);
        Assert.True(result.AutoRenewing);
        Assert.True(result.TestPurchase);
        Assert.False(result.Acknowledged);
        var error = await Assert.ThrowsAsync<BillingValidationException>(() => provider.VerifyAsync("receipt-fixture", "other-account", default));
        Assert.Equal("billing.owner_mismatch", error.Code);
    }

    [Fact]
    public async Task CredentialFailure_DoesNotExposeOriginalExceptionOrSendRequest()
    {
        using var http = new HttpClient(new Handler(_ => throw new InvalidOperationException("must-not-send")));
        var provider = Provider(http, new Credentials(new InvalidOperationException("private-credential-fixture")));
        var error = await Assert.ThrowsAsync<BillingValidationException>(() => provider.VerifyAsync("receipt-fixture", "account", default));
        Assert.Equal("billing.provider_unavailable", error.Code);
        Assert.DoesNotContain("private-credential-fixture", error.ToString());
        Assert.Null(error.InnerException);
    }

    [Theory]
    [InlineData(false, "billing.provider_unavailable")]
    [InlineData(true, "billing.provider_timeout")]
    public async Task TransportFailure_DoesNotExposePurchaseToken(bool timeout, string code)
    {
        using var http = new HttpClient(new Handler(_ => timeout
            ? throw new TaskCanceledException("private-receipt-url-fixture")
            : throw new HttpRequestException("private-receipt-url-fixture")));
        var error = await Assert.ThrowsAsync<BillingValidationException>(() => Provider(http).VerifyAsync("receipt-fixture", "account", default));
        Assert.Equal(code, error.Code);
        Assert.DoesNotContain("private-receipt-url-fixture", error.ToString());
    }

    [Fact]
    public async Task CallerCancellation_IsNotTranslatedToProviderError()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        using var http = new HttpClient(new Handler(_ => throw new OperationCanceledException(source.Token)));
        var provider = Provider(http, new Credentials(new OperationCanceledException(source.Token)));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.VerifyAsync("receipt-fixture", "account", source.Token));
    }

    [Fact]
    public async Task FailedAcknowledgement_RemainsRetryable()
    {
        using var http = new HttpClient(new Handler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))));
        var error = await Assert.ThrowsAsync<BillingValidationException>(() => Provider(http).AcknowledgeAsync("test.monthly", "receipt-fixture", default));
        Assert.Equal("billing.acknowledgement_failed", error.Code);
    }

    private static GooglePlayBillingProvider Provider(HttpClient http, IGooglePlayAccessTokenProvider? credentials = null) =>
        new(http, credentials ?? new Credentials(), new GooglePlayOptions
        { Enabled = true, PackageName = "br.com.equilibrafit.app.plusplus", ProductIds = ["test.monthly"] });

    private sealed class Credentials(Exception? error = null) : IGooglePlayAccessTokenProvider
    {
        public Task<string> GetAsync(CancellationToken ct) => error == null ? Task.FromResult("test-only-credential") : Task.FromException<string>(error);
    }

    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }
}
