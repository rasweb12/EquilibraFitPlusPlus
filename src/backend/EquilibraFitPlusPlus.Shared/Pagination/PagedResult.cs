namespace EquilibraFitPlusPlus.Shared.Pagination;

/// <summary>
/// Represents a paginated response.
/// </summary>
public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalItems)
{
    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalItems / PageSize);
}
