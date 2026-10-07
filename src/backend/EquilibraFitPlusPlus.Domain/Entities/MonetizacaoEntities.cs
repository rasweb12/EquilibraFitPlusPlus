using EquilibraFitPlusPlus.Domain.Common;
using EquilibraFitPlusPlus.Domain.Enums;

namespace EquilibraFitPlusPlus.Domain.Entities;

/// <summary>
/// Commercial subscription prepared for premium evolution.
/// </summary>
public sealed class Assinatura : AuditableEntity, ITenantEntity, ISoftDelete
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>User identifier.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Commercial plan code.</summary>
    public string PlanoCodigo { get; set; } = "FREE";

    /// <summary>Subscription status.</summary>
    public StatusAssinatura Status { get; set; } = StatusAssinatura.Ativa;

    /// <summary>Billing period start.</summary>
    public DateOnly InicioEm { get; set; }

    /// <summary>Billing period end.</summary>
    public DateOnly? TerminaEm { get; set; }

    /// <summary>External provider identifier.</summary>
    public string? ProviderId { get; set; }

    public string? Plataforma { get; set; }
    public string? ProductId { get; set; }
    public string? PurchaseTokenHash { get; set; }
    public string? PurchaseTokenEncrypted { get; set; }
    public string? OrderId { get; set; }
    public string? EstadoCompra { get; set; }
    public DateTimeOffset? InicioUtc { get; set; }
    public DateTimeOffset? ExpiracaoUtc { get; set; }
    public bool AutoRenovacao { get; set; }
    public DateTimeOffset? CanceladoEm { get; set; }
    public DateTimeOffset? UltimoProcessamentoEm { get; set; }
    public string? UltimoEventoId { get; set; }
    public bool AmbienteTeste { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? ExcluidoEm { get; set; }

    /// <inheritdoc />
    public Guid? ExcluidoPor { get; set; }

    /// <inheritdoc />
    public string? MotivoExclusao { get; set; }
}

/// <summary>
/// Payment record.
/// </summary>
public sealed class Pagamento : AuditableEntity, ITenantEntity
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>Subscription identifier.</summary>
    public Guid AssinaturaId { get; set; }

    /// <summary>Payment provider.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Payment method.</summary>
    public string Metodo { get; set; } = string.Empty;

    /// <summary>Amount.</summary>
    public decimal Valor { get; set; }

    /// <summary>Currency.</summary>
    public string Moeda { get; set; } = "BRL";

    /// <summary>Payment status.</summary>
    public StatusPagamento Status { get; set; } = StatusPagamento.Pendente;

    /// <summary>External transaction identifier.</summary>
    public string? TransacaoExternaId { get; set; }
}

/// <summary>
/// Coupon prepared for premium and marketplace strategies.
/// </summary>
public sealed class Cupom : AuditableEntity, ITenantEntity, ISoftDelete
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>Coupon code.</summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>Discount percentage.</summary>
    public decimal? PercentualDesconto { get; set; }

    /// <summary>Fixed discount amount.</summary>
    public decimal? ValorDesconto { get; set; }

    /// <summary>Expiration date.</summary>
    public DateOnly? ExpiraEm { get; set; }

    /// <summary>Maximum usage count.</summary>
    public int? UsoMaximo { get; set; }

    /// <summary>Current usage count.</summary>
    public int UsoAtual { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? ExcluidoEm { get; set; }

    /// <inheritdoc />
    public Guid? ExcluidoPor { get; set; }

    /// <inheritdoc />
    public string? MotivoExclusao { get; set; }
}
