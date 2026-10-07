using EquilibraFitPlusPlus.Application.Abstractions.Billing;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Domain.Enums;
using EquilibraFitPlusPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EquilibraFitPlusPlus.Infrastructure.Billing;

public sealed class GooglePlayBillingService(
    EquilibraFitPlusPlusDbContext db, IPaymentProvider provider, BillingTokenProtector protector,
    TimeProvider clock, ILogger<GooglePlayBillingService> logger) : IBillingService
{
    public async Task<BillingEntitlement> ProcessAsync(Guid tenantId, Guid usuarioId, string token, string eventId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 4096 || string.IsNullOrWhiteSpace(eventId) || eventId.Length > 160)
            throw new BillingValidationException("billing.invalid_request", "Compra invalida.");
        if (!await db.Usuarios.AnyAsync(x => x.Id == usuarioId && x.TenantId == tenantId && x.Status == UsuarioStatus.Ativo, ct))
            throw new BillingValidationException("billing.owner_invalid", "Usuario indisponivel.");
        var hash = BillingTokenProtector.Hash(token);
        var existing = await db.Assinaturas.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.PurchaseTokenHash == hash, ct);
        if (existing != null && (existing.UsuarioId != usuarioId || existing.TenantId != tenantId || existing.ExcluidoEm != null))
            throw new BillingValidationException("billing.owner_mismatch", "A compra pertence a outra conta.");
        var processed = await db.BillingEvents.SingleOrDefaultAsync(x => x.EventId == eventId, ct);
        if (processed != null)
        {
            if (existing == null || processed.AssinaturaId != existing.Id)
                throw new BillingValidationException("billing.event_mismatch", "Evento invalido.");
        }

        var verified = await provider.VerifyAsync(token, BillingTokenProtector.AccountId(usuarioId), ct);
        var now = clock.GetUtcNow();
        if (!string.IsNullOrEmpty(verified.LinkedPurchaseToken))
        {
            var linkedHash = BillingTokenProtector.Hash(verified.LinkedPurchaseToken);
            var old = await db.Assinaturas.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.PurchaseTokenHash == linkedHash, ct);
            if (old != null && old.Id != existing?.Id)
            {
                if (old.UsuarioId != usuarioId || old.TenantId != tenantId)
                    throw new BillingValidationException("billing.owner_mismatch", "A compra anterior pertence a outra conta.");
                old.Status = StatusAssinatura.Cancelada;
                old.ExpiracaoUtc = now;
                old.CanceladoEm = now;
                old.EstadoCompra = "REPLACED";
            }
        }

        var subscription = existing ?? new Assinatura { TenantId = tenantId, UsuarioId = usuarioId };
        if (existing == null) db.Assinaturas.Add(subscription);
        subscription.Plataforma = "GOOGLE_PLAY";
        subscription.ProviderId = "GOOGLE_PLAY";
        subscription.PlanoCodigo = "PREMIUM";
        subscription.ProductId = verified.ProductId;
        subscription.PurchaseTokenHash = hash;
        subscription.PurchaseTokenEncrypted = protector.Encrypt(token);
        subscription.OrderId = verified.OrderId;
        subscription.EstadoCompra = verified.State;
        subscription.InicioUtc = verified.StartedAt.ToUniversalTime();
        subscription.ExpiracaoUtc = verified.ExpiresAt.ToUniversalTime();
        subscription.InicioEm = DateOnly.FromDateTime(verified.StartedAt.UtcDateTime);
        subscription.TerminaEm = DateOnly.FromDateTime(verified.ExpiresAt.UtcDateTime);
        subscription.AutoRenovacao = verified.AutoRenewing;
        subscription.AmbienteTeste = verified.TestPurchase;
        subscription.CanceladoEm = verified.State == "SUBSCRIPTION_STATE_CANCELED" ? subscription.CanceladoEm ?? now : null;
        subscription.Status = verified.GrantsPremium(now) ? StatusAssinatura.Ativa :
            verified.State == "SUBSCRIPTION_STATE_ON_HOLD" ? StatusAssinatura.Atrasada : StatusAssinatura.Expirada;
        subscription.UltimoProcessamentoEm = now;
        subscription.UltimoEventoId = eventId;
        if (processed == null)
            db.BillingEvents.Add(new BillingEvent
            {
                TenantId = tenantId, AssinaturaId = subscription.Id, EventId = eventId, State = verified.State
            });
        await db.SaveChangesAsync(ct);
        if (!verified.Acknowledged && verified.GrantsPremium(now))
            await provider.AcknowledgeAsync(verified.ProductId, token, ct);
        logger.LogInformation("Google Play subscription processed: {SubscriptionId} {State} {EventId}", subscription.Id, verified.State, eventId);
        return Result(subscription);
    }

    public async Task ProcessNotificationAsync(string token, string eventId, CancellationToken ct)
    {
        var hash = BillingTokenProtector.Hash(token);
        var subscription = await db.Assinaturas.SingleOrDefaultAsync(x => x.PurchaseTokenHash == hash, ct);
        if (subscription == null) return; // Client verification binds new receipts to the authenticated account.
        await ProcessAsync(subscription.TenantId, subscription.UsuarioId, token, eventId, ct);
    }

    private BillingEntitlement Result(Assinatura subscription) => new(
        subscription.Status == StatusAssinatura.Ativa && subscription.ExpiracaoUtc > clock.GetUtcNow(),
        subscription.ProductId!, subscription.EstadoCompra!, subscription.ExpiracaoUtc!.Value, subscription.AutoRenovacao);
}
