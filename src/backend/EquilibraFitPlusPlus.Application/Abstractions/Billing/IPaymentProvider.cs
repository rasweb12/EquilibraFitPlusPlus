namespace EquilibraFitPlusPlus.Application.Abstractions.Billing;

public interface IPaymentProvider
{
    Task<VerifiedSubscription> VerifyAsync(string purchaseToken, string accountId, CancellationToken ct);
    Task AcknowledgeAsync(string productId, string purchaseToken, CancellationToken ct);
}

public sealed record VerifiedSubscription(
    string ProductId, string State, DateTimeOffset StartedAt, DateTimeOffset ExpiresAt,
    bool AutoRenewing, bool TestPurchase, bool Acknowledged, string? OrderId, string? LinkedPurchaseToken)
{
    public bool GrantsPremium(DateTimeOffset now) =>
        ExpiresAt > now && State is "SUBSCRIPTION_STATE_ACTIVE" or "SUBSCRIPTION_STATE_IN_GRACE_PERIOD" or "SUBSCRIPTION_STATE_CANCELED";
}

public sealed class BillingValidationException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
