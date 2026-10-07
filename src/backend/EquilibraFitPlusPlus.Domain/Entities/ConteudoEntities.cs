using EquilibraFitPlusPlus.Domain.Common;

namespace EquilibraFitPlusPlus.Domain.Entities;

/// <summary>
/// Predefined meal template.
/// </summary>
public sealed class RefeicaoPredefinida : AuditableEntity, ITenantEntity, ISoftDelete
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>Meal name.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Meal description.</summary>
    public string? Descricao { get; set; }

    /// <summary>Indicates whether it is visible to users.</summary>
    public bool Publicada { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? ExcluidoEm { get; set; }

    /// <inheritdoc />
    public Guid? ExcluidoPor { get; set; }

    /// <inheritdoc />
    public string? MotivoExclusao { get; set; }

    /// <summary>Meal template items.</summary>
    public ICollection<ItemRefeicaoPredefinida> Itens { get; set; } = [];
}

/// <summary>
/// Food item inside a predefined meal.
/// </summary>
public sealed class ItemRefeicaoPredefinida : AuditableEntity, ITenantEntity
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>Predefined meal identifier.</summary>
    public Guid RefeicaoPredefinidaId { get; set; }

    /// <summary>Food name.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Quantity.</summary>
    public decimal Quantidade { get; set; }

    /// <summary>Unit.</summary>
    public string Unidade { get; set; } = "g";
}
