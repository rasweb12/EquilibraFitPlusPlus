namespace EquilibraFitPlusPlus.Domain.Common;

/// <summary>
/// Marks an entity as soft deletable.
/// </summary>
public interface ISoftDelete
{
    /// <summary>
    /// UTC deletion date.
    /// </summary>
    DateTimeOffset? ExcluidoEm { get; set; }

    /// <summary>
    /// User responsible for deletion.
    /// </summary>
    Guid? ExcluidoPor { get; set; }

    /// <summary>
    /// Deletion reason.
    /// </summary>
    string? MotivoExclusao { get; set; }
}
