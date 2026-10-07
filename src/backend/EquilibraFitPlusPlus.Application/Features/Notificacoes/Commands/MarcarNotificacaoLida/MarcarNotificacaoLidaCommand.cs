using EquilibraFitPlusPlus.Contracts.Notificacoes;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Notificacoes.Commands.MarcarNotificacaoLida;

/// <summary>
/// Command used to mark a notification as read.
/// </summary>
public sealed record MarcarNotificacaoLidaCommand(Guid TenantId, Guid UsuarioId, Guid NotificacaoId)
    : IRequest<Result<NotificacaoResponse>>;
