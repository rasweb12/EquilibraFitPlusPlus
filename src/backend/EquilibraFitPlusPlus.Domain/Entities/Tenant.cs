using EquilibraFitPlusPlus.Domain.Common;

namespace EquilibraFitPlusPlus.Domain.Entities;

/// <summary>
/// Represents a tenant for individual, corporate, academy, or professional usage.
/// </summary>
public sealed class Tenant : AuditableEntity, ISoftDelete
{
    /// <summary>Tenant name.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Tenant type.</summary>
    public string Tipo { get; set; } = "Individual";

    /// <summary>Indicates whether the tenant is active.</summary>
    public bool Ativo { get; set; } = true;

    /// <inheritdoc />
    public DateTimeOffset? ExcluidoEm { get; set; }

    /// <inheritdoc />
    public Guid? ExcluidoPor { get; set; }

    /// <inheritdoc />
    public string? MotivoExclusao { get; set; }
}
