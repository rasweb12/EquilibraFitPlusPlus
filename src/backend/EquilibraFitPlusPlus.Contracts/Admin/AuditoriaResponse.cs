namespace EquilibraFitPlusPlus.Contracts.Admin;

/// <summary>
/// Administrative audit log response.
/// </summary>
public sealed record AuditoriaResponse(
    Guid Id,
    Guid? UsuarioId,
    string Acao,
    string Entidade,
    Guid? EntidadeId,
    string? MetadadosJson,
    string? Ip,
    string? UserAgent,
    DateTimeOffset CriadoEm);
