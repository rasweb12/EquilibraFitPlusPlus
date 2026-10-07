using EquilibraFitPlusPlus.Domain.Common;

namespace EquilibraFitPlusPlus.Domain.Entities;

/// <summary>Deduplication and safe subscription history; never stores raw purchase tokens.</summary>
public sealed class BillingEvent : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid AssinaturaId { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public Assinatura? Assinatura { get; set; }
}
