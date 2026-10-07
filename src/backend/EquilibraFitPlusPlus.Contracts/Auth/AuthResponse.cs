namespace EquilibraFitPlusPlus.Contracts.Auth;

/// <summary>
/// Authentication response with access and refresh tokens.
/// </summary>
public sealed record AuthResponse(
    string AccessToken,
    int ExpiresIn,
    string RefreshToken,
    UsuarioAutenticadoResponse Usuario);

/// <summary>
/// Authenticated user summary.
/// </summary>
public sealed record UsuarioAutenticadoResponse(Guid Id, Guid TenantId, string Nome, string Email, string Role);
