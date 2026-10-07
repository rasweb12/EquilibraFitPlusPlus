namespace EquilibraFitPlusPlus.Contracts.Admin;

/// <summary>
/// Administrative user summary response.
/// </summary>
public sealed record AdminUsuarioResumoResponse(
    Guid Id,
    string Nome,
    string Email,
    string Status,
    string Role,
    DateTimeOffset CriadoEm,
    DateTimeOffset? UltimoLoginEm,
    bool PerfilPreenchido);

/// <summary>
/// Administrative response returned after revoking a user's active sessions.
/// </summary>
public sealed record RevogarSessoesUsuarioResponse(
    Guid UsuarioId,
    int SessoesRevogadas,
    string Mensagem);
