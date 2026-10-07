namespace EquilibraFitPlusPlus.Admin.Services;

/// <summary>
/// Configuration used by the administrative web application to reach the backend API.
/// </summary>
public sealed class AdminApiOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "AdminApi";

    /// <summary>Base URL of the EquilibraFit++ backend API.</summary>
    public string BaseUrl { get; set; } = "http://localhost:5158";
}
