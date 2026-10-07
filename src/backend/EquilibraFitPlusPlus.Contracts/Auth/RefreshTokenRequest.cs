namespace EquilibraFitPlusPlus.Contracts.Auth;

/// <summary>
/// Request used to rotate a refresh token.
/// </summary>
public sealed record RefreshTokenRequest(string RefreshToken);
