using EquilibraFitPlusPlus.Domain.Common;
using EquilibraFitPlusPlus.Domain.Enums;

namespace EquilibraFitPlusPlus.Domain.Entities;

/// <summary>
/// Represents a confirmed food log.
/// </summary>
public sealed class RegistroAlimentar : AuditableEntity, ITenantEntity, ISoftDelete
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>User identifier.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Meal date and time.</summary>
    public DateTimeOffset DataHora { get; set; }

    /// <summary>Meal type.</summary>
    public TipoRefeicao TipoRefeicao { get; set; }

    /// <summary>Meal origin.</summary>
    public OrigemRegistroAlimentar Origem { get; set; }

    /// <summary>Total calories.</summary>
    public decimal CaloriasTotal { get; set; }

    /// <summary>Total protein in grams.</summary>
    public decimal ProteinaTotalG { get; set; }

    /// <summary>Total carbohydrate in grams.</summary>
    public decimal CarboidratoTotalG { get; set; }

    /// <summary>Total fat in grams.</summary>
    public decimal GorduraTotalG { get; set; }

    /// <summary>Indicates whether the user confirmed the record.</summary>
    public bool ConfirmadoPeloUsuario { get; set; }

    /// <summary>Meal owner.</summary>
    public Usuario? Usuario { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? ExcluidoEm { get; set; }

    /// <inheritdoc />
    public Guid? ExcluidoPor { get; set; }

    /// <inheritdoc />
    public string? MotivoExclusao { get; set; }

    /// <summary>Meal items.</summary>
    public ICollection<ItemAlimentar> Itens { get; set; } = [];

    /// <summary>Optional AI image analysis.</summary>
    public AnaliseRefeicaoImagem? AnaliseImagem { get; set; }
}

/// <summary>
/// Represents a food item inside a meal record.
/// </summary>
public sealed class ItemAlimentar : AuditableEntity, ITenantEntity
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>Meal record identifier.</summary>
    public Guid RegistroAlimentarId { get; set; }

    /// <summary>Food name.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Quantity.</summary>
    public decimal Quantidade { get; set; }

    /// <summary>Unit.</summary>
    public string Unidade { get; set; } = "g";

    /// <summary>Calories.</summary>
    public decimal Calorias { get; set; }

    /// <summary>Protein in grams.</summary>
    public decimal ProteinaG { get; set; }

    /// <summary>Carbohydrate in grams.</summary>
    public decimal CarboidratoG { get; set; }

    /// <summary>Fat in grams.</summary>
    public decimal GorduraG { get; set; }

    /// <summary>Nutritional data source.</summary>
    public string? FonteNutricional { get; set; }

    /// <summary>Meal record navigation.</summary>
    public RegistroAlimentar? RegistroAlimentar { get; set; }
}

/// <summary>
/// Represents an AI meal image analysis.
/// </summary>
public sealed class AnaliseRefeicaoImagem : AuditableEntity, ITenantEntity
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>User identifier.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Meal record identifier created after confirmation.</summary>
    public Guid? RegistroAlimentarId { get; set; }

    /// <summary>Blob URI for the stored image.</summary>
    public string? BlobUri { get; set; }

    /// <summary>Average confidence percentage.</summary>
    public decimal ConfiancaMedia { get; set; }

    /// <summary>Vision model version.</summary>
    public string ModeloVisao { get; set; } = string.Empty;

    /// <summary>Analysis status.</summary>
    public StatusAnaliseImagem Status { get; set; } = StatusAnaliseImagem.Pendente;

    /// <summary>Structured AI result as JSON.</summary>
    public string ResultadoJson { get; set; } = "{}";
}
