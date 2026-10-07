namespace EquilibraFitPlusPlus.Admin.Services;

/// <summary>
/// Represents a friendly administrative API failure.
/// </summary>
public sealed class AdminApiException : Exception
{
    /// <summary>Initializes a new exception with a safe user-facing message.</summary>
    public AdminApiException(string message, string? code = null)
        : base(message)
    {
        Code = code;
    }

    /// <summary>Initializes a new exception with a safe user-facing message and an inner exception for diagnostics.</summary>
    public AdminApiException(string message, string? code, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }

    /// <summary>Optional API error code.</summary>
    public string? Code { get; }
}
