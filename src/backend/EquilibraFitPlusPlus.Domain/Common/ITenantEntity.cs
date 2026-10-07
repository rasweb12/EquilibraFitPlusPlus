namespace EquilibraFitPlusPlus.Domain.Common;

/// <summary>
/// Marks an entity as tenant scoped.
/// </summary>
public interface ITenantEntity
{
    /// <summary>
    /// Tenant identifier.
    /// </summary>
    Guid TenantId { get; set; }
}
