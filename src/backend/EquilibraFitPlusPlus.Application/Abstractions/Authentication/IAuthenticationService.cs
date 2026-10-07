using EquilibraFitPlusPlus.Contracts.Auth;
using EquilibraFitPlusPlus.Shared.Results;

namespace EquilibraFitPlusPlus.Application.Abstractions.Authentication;

/// <summary>
/// Provides authentication operations backed by Supabase Auth.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Registers a new user and returns authentication tokens.
    /// </summary>
    Task<Result<AuthResponse>> CadastrarAsync(CadastrarUsuarioRequest request, string? ipAddress, CancellationToken cancellationToken);

    /// <summary>
    /// Authenticates an existing user and returns authentication tokens.
    /// </summary>
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken);

    /// <summary>
    /// Rotates a refresh token and returns new tokens.
    /// </summary>
    Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken);
}
