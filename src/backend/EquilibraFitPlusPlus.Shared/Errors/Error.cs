namespace EquilibraFitPlusPlus.Shared.Errors;

/// <summary>
/// Represents an application error that can be safely returned to API clients.
/// </summary>
public sealed record Error(string Code, string Message, string? Field = null)
{
    /// <summary>
    /// Creates an unexpected error.
    /// </summary>
    public static Error Unexpected(string message) => new("unexpected_error", message);

    /// <summary>
    /// Creates a validation error.
    /// </summary>
    public static Error Validation(string field, string message) => new("validation_error", message, field);
}
