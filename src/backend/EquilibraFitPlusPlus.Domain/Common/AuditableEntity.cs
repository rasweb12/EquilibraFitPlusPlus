namespace EquilibraFitPlusPlus.Domain.Common;

/// <summary>
/// Base entity with auditing and optimistic concurrency fields.
/// </summary>
public abstract class AuditableEntity : Entity
{
    /// <summary>
    /// UTC creation date.
    /// </summary>
    public DateTimeOffset CriadoEm { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// User responsible for creation.
    /// </summary>
    public Guid? CriadoPor { get; set; }

    /// <summary>
    /// UTC update date.
    /// </summary>
    public DateTimeOffset? AtualizadoEm { get; set; }

    /// <summary>
    /// User responsible for the latest update.
    /// </summary>
    public Guid? AtualizadoPor { get; set; }

    /// <summary>
    /// Application-managed token used for optimistic concurrency across providers.
    /// </summary>
    public byte[] RowVersion { get; set; } = [];
}
