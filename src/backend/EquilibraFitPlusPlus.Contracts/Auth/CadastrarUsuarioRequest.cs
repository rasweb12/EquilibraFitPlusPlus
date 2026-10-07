namespace EquilibraFitPlusPlus.Contracts.Auth;

/// <summary>
/// Request used to register a new user.
/// </summary>
public sealed record CadastrarUsuarioRequest(
    string Nome,
    string Email,
    string Senha,
    IReadOnlyCollection<ConsentimentoRequest> Aceites);
