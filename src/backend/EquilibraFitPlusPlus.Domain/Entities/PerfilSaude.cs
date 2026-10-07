using EquilibraFitPlusPlus.Domain.Common;
using EquilibraFitPlusPlus.Domain.Enums;

namespace EquilibraFitPlusPlus.Domain.Entities;

/// <summary>
/// Stores health profile data used to personalize recommendations.
/// </summary>
public sealed class PerfilSaude : AuditableEntity, ITenantEntity, ISoftDelete
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>User identifier.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Date of birth.</summary>
    public DateOnly DataNascimento { get; set; }

    /// <summary>Biological sex used for metabolic calculations.</summary>
    public SexoBiologico SexoBiologico { get; set; }

    /// <summary>Height in centimeters.</summary>
    public decimal AlturaCm { get; set; }

    /// <summary>Current weight in kilograms.</summary>
    public decimal PesoAtualKg { get; set; }

    /// <summary>Main health objective.</summary>
    public ObjetivoSaude Objetivo { get; set; }

    /// <summary>Physical activity level.</summary>
    public NivelAtividade NivelAtividade { get; set; }

    /// <summary>Available workout days per week.</summary>
    public byte DiasTreinoSemana { get; set; }

    /// <summary>Food preferences stored as JSON.</summary>
    public string? PreferenciasJson { get; set; }

    /// <summary>Restrictions stored as JSON.</summary>
    public string? RestricoesJson { get; set; }

    /// <summary>Medical or routine observations stored as JSON.</summary>
    public string? ObservacoesJson { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? ExcluidoEm { get; set; }

    /// <inheritdoc />
    public Guid? ExcluidoPor { get; set; }

    /// <inheritdoc />
    public string? MotivoExclusao { get; set; }

    /// <summary>Profile owner.</summary>
    public Usuario? Usuario { get; set; }
}
