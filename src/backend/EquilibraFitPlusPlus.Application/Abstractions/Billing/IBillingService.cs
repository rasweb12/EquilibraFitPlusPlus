namespace EquilibraFitPlusPlus.Application.Abstractions.Billing;

public interface IBillingService
{
    Task<BillingEntitlement> ProcessAsync(Guid tenantId, Guid usuarioId, string token, string eventId, CancellationToken ct);
    Task ProcessNotificationAsync(string token, string eventId, CancellationToken ct);
}

public sealed record BillingEntitlement(bool Premium, string ProductId, string State, DateTimeOffset ExpiresAt, bool AutoRenewing);
