namespace EquilibraFitPlusPlus.Contracts.Auth;

/// <summary>
/// Consent accepted by the user during registration.
/// </summary>
public sealed record ConsentimentoRequest(string Tipo, string Versao);
