namespace EquilibraFitPlusPlus.Contracts.Common;

/// <summary>
/// Pagination request shared by listing endpoints.
/// </summary>
public sealed record PagedRequest(int Page = 1, int PageSize = 20);
