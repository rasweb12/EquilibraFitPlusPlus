namespace EquilibraFitPlusPlus.Domain.Common;

/// <summary>
/// Base entity with a strongly typed identifier.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Entity identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
}
