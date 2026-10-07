namespace EquilibraFitPlusPlus.Contracts.AiCoach;

/// <summary>
/// Request used to send a message to the AI Coach.
/// </summary>
public sealed record EnviarMensagemCoachRequest(Guid? SessaoId, string Mensagem);
