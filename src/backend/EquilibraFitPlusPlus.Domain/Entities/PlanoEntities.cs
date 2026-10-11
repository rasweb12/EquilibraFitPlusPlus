using EquilibraFitPlusPlus.Domain.Common;
using EquilibraFitPlusPlus.Domain.Enums;

namespace EquilibraFitPlusPlus.Domain.Entities;

/// <summary>
/// Represents a versioned personalized user plan.
/// </summary>
public sealed class PlanoUsuario : AuditableEntity, ITenantEntity, ISoftDelete
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>User identifier.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Plan version for historical tracking.</summary>
    public int Versao { get; set; }

    /// <summary>Plan status.</summary>
    public StatusPlano Status { get; set; } = StatusPlano.Ativo;

    /// <summary>Daily calories.</summary>
    public int CaloriasDia { get; set; }

    /// <summary>Safe weekly goal in kilograms.</summary>
    public decimal ObjetivoSemanalKg { get; set; }

    /// <summary>Friendly explanation shown to users.</summary>
    public string Explicacao { get; set; } = string.Empty;

    /// <summary>Generation source.</summary>
    public string FonteGeracao { get; set; } = "Hibrido";

    /// <summary>AI model version used to propose the plan.</summary>
    public string? ModeloIaVersao { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? ExcluidoEm { get; set; }

    /// <inheritdoc />
    public Guid? ExcluidoPor { get; set; }

    /// <inheritdoc />
    public string? MotivoExclusao { get; set; }

    /// <summary>Plan owner.</summary>
    public Usuario? Usuario { get; set; }

    /// <summary>Plan nutritional goals.</summary>
    public ICollection<MetaNutricional> MetasNutricionais { get; set; } = [];

    /// <summary>Structured meal suggestions that belong to this plan version.</summary>
    public ICollection<PlanoRefeicao> Refeicoes { get; set; } = [];
}

/// <summary>
/// A suggested meal persisted with its owning plan version.
/// </summary>
public sealed class PlanoRefeicao : AuditableEntity, ITenantEntity
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>Owning plan identifier.</summary>
    public Guid PlanoUsuarioId { get; set; }

    /// <summary>Display order inside the plan.</summary>
    public int Ordem { get; set; }

    /// <summary>Meal name.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Educational meal description.</summary>
    public string Descricao { get; set; } = string.Empty;

    /// <summary>Estimated calories for the suggestion.</summary>
    public int Calorias { get; set; }

    /// <summary>Optional, trusted image URL. The AI never populates this field.</summary>
    public string? ImagemUrl { get; set; }

    /// <summary>JSON payload with structured alternatives supplied by the plan generator.</summary>
    public string AlternativasJson { get; set; } = "[]";

    /// <summary>Optional JSON payload with a reviewed quick recipe.</summary>
    public string? ReceitaRapidaJson { get; set; }

    /// <summary>Owning plan navigation.</summary>
    public PlanoUsuario? PlanoUsuario { get; set; }
}

/// <summary>
/// Defines nutritional targets for a plan.
/// </summary>
public sealed class MetaNutricional : AuditableEntity, ITenantEntity
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>Plan identifier.</summary>
    public Guid PlanoUsuarioId { get; set; }

    /// <summary>Protein target in grams.</summary>
    public decimal ProteinaG { get; set; }

    /// <summary>Carbohydrate target in grams.</summary>
    public decimal CarboidratoG { get; set; }

    /// <summary>Fat target in grams.</summary>
    public decimal GorduraG { get; set; }

    /// <summary>Fiber target in grams.</summary>
    public decimal? FibraG { get; set; }

    /// <summary>Water target in milliliters.</summary>
    public int? AguaMl { get; set; }

    /// <summary>Plan navigation.</summary>
    public PlanoUsuario? PlanoUsuario { get; set; }
}
