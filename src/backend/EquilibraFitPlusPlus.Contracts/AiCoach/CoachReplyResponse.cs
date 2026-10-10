namespace EquilibraFitPlusPlus.Contracts.AiCoach;

/// <summary>
/// AI Coach reply response.
/// </summary>
public sealed record CoachReplyResponse(
    Guid SessaoId,
    CoachMessageResponse MensagemUsuario,
    CoachMessageResponse MensagemCoach,
    string AvisoSaude,
    bool FallbackUsed = false);
