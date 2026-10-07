namespace EquilibraFitPlusPlus.Application.Abstractions.Data;

/// <summary>
/// Represents a persistence boundary for transactional application operations.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists pending changes.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
