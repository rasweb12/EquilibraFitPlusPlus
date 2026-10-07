namespace EquilibraFitPlusPlus.Contracts.Notificacoes;

/// <summary>
/// User notification response.
/// </summary>
public sealed record NotificacaoResponse(
    Guid Id,
    Guid UsuarioId,
    string Titulo,
    string Mensagem,
    string Status,
    DateTimeOffset CriadoEm);

/// <summary>
/// Request used by administrators to create a notification.
/// </summary>
public sealed record CriarNotificacaoRequest(
    Guid UsuarioId,
    string Titulo,
    string Mensagem);
