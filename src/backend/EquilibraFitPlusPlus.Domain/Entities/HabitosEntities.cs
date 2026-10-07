using EquilibraFitPlusPlus.Domain.Common;

namespace EquilibraFitPlusPlus.Domain.Entities;

/// <summary>
/// Represents a daily habits log for a user.
/// </summary>
public sealed class RegistroHabitos : AuditableEntity, ITenantEntity, ISoftDelete
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>User identifier.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Log date.</summary>
    public DateOnly Data { get; set; }

    /// <summary>Water intake in milliliters.</summary>
    public int AguaMl { get; set; }

    /// <summary>Daily water goal in milliliters.</summary>
    public int MetaAguaMl { get; set; } = 2700;

    /// <summary>Sleep duration in hours.</summary>
    public decimal SonoHoras { get; set; }

    /// <summary>Daily sleep goal in hours.</summary>
    public decimal MetaSonoHoras { get; set; } = 8m;

    /// <summary>User mood score from 1 to 5.</summary>
    public int Humor { get; set; } = 3;

    /// <summary>Whether meditation was completed.</summary>
    public bool MeditacaoRealizada { get; set; }

    /// <summary>Whether stretching was completed.</summary>
    public bool AlongamentoRealizado { get; set; }

    /// <summary>Habit owner.</summary>
    public Usuario? Usuario { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? ExcluidoEm { get; set; }

    /// <inheritdoc />
    public Guid? ExcluidoPor { get; set; }

    /// <inheritdoc />
    public string? MotivoExclusao { get; set; }
}
