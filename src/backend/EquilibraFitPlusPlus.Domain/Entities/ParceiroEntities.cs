using EquilibraFitPlusPlus.Domain.Common;
using EquilibraFitPlusPlus.Domain.Enums;

namespace EquilibraFitPlusPlus.Domain.Entities;

/// <summary>
/// Marketplace or ecosystem partner.
/// </summary>
public sealed class Parceiro : AuditableEntity, ITenantEntity, ISoftDelete
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>Partner name.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Partner type.</summary>
    public TipoParceiro Tipo { get; set; }

    /// <summary>Partner status.</summary>
    public StatusParceiro Status { get; set; } = StatusParceiro.Rascunho;

    /// <summary>Contact email.</summary>
    public string? EmailContato { get; set; }

    /// <summary>Document number.</summary>
    public string? Documento { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? ExcluidoEm { get; set; }

    /// <inheritdoc />
    public Guid? ExcluidoPor { get; set; }

    /// <inheritdoc />
    public string? MotivoExclusao { get; set; }
}

/// <summary>
/// Verified professional profile.
/// </summary>
public sealed class Profissional : AuditableEntity, ITenantEntity, ISoftDelete
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>Partner identifier.</summary>
    public Guid ParceiroId { get; set; }

    /// <summary>Professional full name.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Professional council or certification.</summary>
    public string? RegistroProfissional { get; set; }

    /// <summary>Specialty.</summary>
    public string? Especialidade { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? ExcluidoEm { get; set; }

    /// <inheritdoc />
    public Guid? ExcluidoPor { get; set; }

    /// <inheritdoc />
    public string? MotivoExclusao { get; set; }
}
