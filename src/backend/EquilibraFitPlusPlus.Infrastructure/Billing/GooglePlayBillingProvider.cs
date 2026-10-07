using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EquilibraFitPlusPlus.Application.Abstractions.Billing;
using Google.Apis.Auth.OAuth2;

namespace EquilibraFitPlusPlus.Infrastructure.Billing;

public interface IGooglePlayAccessTokenProvider
{
    Task<string> GetAsync(CancellationToken ct);
}

public sealed class GooglePlayAccessTokenProvider : IGooglePlayAccessTokenProvider
{
    public async Task<string> GetAsync(CancellationToken ct)
    {
        var credential = (await GoogleCredential.GetApplicationDefaultAsync(ct))
            .CreateScoped("https://www.googleapis.com/auth/androidpublisher");
        return await credential.UnderlyingCredential.GetAccessTokenForRequestAsync(cancellationToken: ct);
    }
}

/// <summary>Server verification using the Google Play Developer API, never client entitlement claims.</summary>
public sealed class GooglePlayBillingProvider(
    HttpClient http, IGooglePlayAccessTokenProvider credentials, GooglePlayOptions options) : IPaymentProvider
{
    public async Task<VerifiedSubscription> VerifyAsync(string purchaseToken, string accountId, CancellationToken ct)
    {
        CheckConfiguration();
        using var request = await RequestAsync(HttpMethod.Get,
            $"applications/{Uri.EscapeDataString(options.PackageName)}/purchases/subscriptionsv2/tokens/{Uri.EscapeDataString(purchaseToken)}", ct);
        using var response = await SendWithoutSensitiveErrorsAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new BillingValidationException("billing.verification_failed", "Nao foi possivel validar a compra no Google Play.");
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var root = json.RootElement;
        if (!root.TryGetProperty("externalAccountIdentifiers", out var account) ||
            !account.TryGetProperty("obfuscatedExternalAccountId", out var accountValue) ||
            !string.Equals(accountValue.GetString(), accountId, StringComparison.Ordinal))
            throw new BillingValidationException("billing.owner_mismatch", "A compra pertence a outra conta.");

        var item = root.GetProperty("lineItems").EnumerateArray()
            .Where(x => options.ProductIds.Contains(x.GetProperty("productId").GetString(), StringComparer.Ordinal))
            .Where(x => x.TryGetProperty("expiryTime", out _))
            .OrderByDescending(x => x.GetProperty("expiryTime").GetDateTimeOffset())
            .Select(x => (JsonElement?)x).FirstOrDefault()
            ?? throw new BillingValidationException("billing.product_invalid", "Produto nao autorizado ou compra pendente.");
        var state = root.GetProperty("subscriptionState").GetString() ?? string.Empty;
        return new VerifiedSubscription(
            item.GetProperty("productId").GetString()!, state,
            root.TryGetProperty("startTime", out var start) ? start.GetDateTimeOffset() : DateTimeOffset.UtcNow,
            item.GetProperty("expiryTime").GetDateTimeOffset(),
            item.TryGetProperty("autoRenewingPlan", out var plan) &&
                plan.TryGetProperty("autoRenewEnabled", out var renewing) && renewing.GetBoolean(),
            root.TryGetProperty("testPurchase", out _),
            root.TryGetProperty("acknowledgementState", out var ack) && ack.GetString() == "ACKNOWLEDGEMENT_STATE_ACKNOWLEDGED",
            item.TryGetProperty("latestSuccessfulOrderId", out var order) ? order.GetString() : null,
            root.TryGetProperty("linkedPurchaseToken", out var linked) ? linked.GetString() : null);
    }

    public async Task AcknowledgeAsync(string productId, string purchaseToken, CancellationToken ct)
    {
        CheckConfiguration();
        using var request = await RequestAsync(HttpMethod.Post,
            $"applications/{Uri.EscapeDataString(options.PackageName)}/purchases/subscriptions/{Uri.EscapeDataString(productId)}/tokens/{Uri.EscapeDataString(purchaseToken)}:acknowledge", ct);
        request.Content = JsonContent.Create(new { });
        using var response = await SendWithoutSensitiveErrorsAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new BillingValidationException("billing.acknowledgement_failed", "A confirmacao da compra sera tentada novamente.");
    }

    private void CheckConfiguration()
    {
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.PackageName) || options.ProductIds.Length == 0)
            throw new BillingValidationException("billing.not_configured", "Assinaturas ainda indisponiveis.");
    }

    private async Task<HttpResponseMessage> SendWithoutSensitiveErrorsAsync(HttpRequestMessage request, CancellationToken ct)
    {
        try { return await http.SendAsync(request, ct); }
        catch (HttpRequestException)
        {
            // Google URLs contain the purchase token; never propagate those exceptions into logs.
            throw new BillingValidationException("billing.provider_unavailable", "Google Play temporariamente indisponivel.");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new BillingValidationException("billing.provider_timeout", "Google Play temporariamente indisponivel.");
        }
    }

    private async Task<HttpRequestMessage> RequestAsync(HttpMethod method, string path, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, new Uri("https://androidpublisher.googleapis.com/androidpublisher/v3/" + path));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await credentials.GetAsync(ct));
        return request;
    }
}
