using EquilibraFitPlusPlus.Domain.Common;

namespace EquilibraFitPlusPlus.Domain.Entities;

/// <summary>
/// Represents a progress log for body evolution.
/// </summary>
public sealed class RegistroEvolucao : AuditableEntity, ITenantEntity, ISoftDelete
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>User identifier.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Log date.</summary>
    public DateOnly Data { get; set; }

    /// <summary>Weight in kilograms.</summary>
    public decimal PesoKg { get; set; }

    /// <summary>Body fat percentage.</summary>
    public decimal? PercentualGordura { get; set; }

    /// <summary>Lean mass percentage.</summary>
    public decimal? PercentualMassaMagra { get; set; }

    /// <summary>User observation.</summary>
    public string? Observacao { get; set; }

    /// <summary>Progress owner.</summary>
    public Usuario? Usuario { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? ExcluidoEm { get; set; }

    /// <inheritdoc />
    public Guid? ExcluidoPor { get; set; }

    /// <inheritdoc />
    public string? MotivoExclusao { get; set; }

    /// <summary>Body measurements.</summary>
    public ICollection<MedidaCorporal> Medidas { get; set; } = [];
}

/// <summary>
/// Represents a body measurement linked to a progress log.
/// </summary>
public sealed class MedidaCorporal : AuditableEntity, ITenantEntity
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>Progress log identifier.</summary>
    public Guid RegistroEvolucaoId { get; set; }

    /// <summary>Measurement name.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Measurement value in centimeters.</summary>
    public decimal ValorCm { get; set; }

    /// <summary>Progress log navigation.</summary>
    public RegistroEvolucao? RegistroEvolucao { get; set; }
}
