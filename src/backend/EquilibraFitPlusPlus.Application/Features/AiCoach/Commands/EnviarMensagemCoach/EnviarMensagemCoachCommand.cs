using EquilibraFitPlusPlus.Contracts.AiCoach;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.AiCoach.Commands.EnviarMensagemCoach;

/// <summary>
/// Command used to send a message to AI Coach.
/// </summary>
public sealed record EnviarMensagemCoachCommand(Guid TenantId, Guid UsuarioId, EnviarMensagemCoachRequest Request)
    : IRequest<Result<CoachReplyResponse>>;
