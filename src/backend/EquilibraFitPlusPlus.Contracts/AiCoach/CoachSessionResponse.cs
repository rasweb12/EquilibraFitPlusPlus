namespace EquilibraFitPlusPlus.Contracts.AiCoach;

/// <summary>
/// AI Coach chat session response.
/// </summary>
public sealed record CoachSessionResponse(
    Guid Id,
    string Titulo,
    DateTimeOffset CriadoEm,
    IReadOnlyCollection<CoachMessageResponse> Mensagens);

/// <summary>
/// AI Coach chat message response.
/// </summary>
public sealed record CoachMessageResponse(Guid Id, string Role, string Conteudo, DateTimeOffset CriadoEm, string? ModeloIa);
