namespace EquilibraFitPlusPlus.Contracts.Auth;

/// <summary>
/// Request used to authenticate a user.
/// </summary>
public sealed record LoginRequest(string Email, string Senha);
