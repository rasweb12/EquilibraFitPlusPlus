namespace EquilibraFitPlusPlus.Contracts.Common;

/// <summary>
/// Standard API error response.
/// </summary>
public sealed record ApiErrorResponse(string TraceId, string Code, string Message, IReadOnlyCollection<ApiFieldError> Details);

/// <summary>
/// Standard field-level API error.
/// </summary>
public sealed record ApiFieldError(string? Field, string Message);
