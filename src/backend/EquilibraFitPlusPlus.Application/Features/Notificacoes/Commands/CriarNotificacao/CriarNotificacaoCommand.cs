using EquilibraFitPlusPlus.Contracts.Notificacoes;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Notificacoes.Commands.CriarNotificacao;

/// <summary>
/// Command used to create a notification.
/// </summary>
public sealed record CriarNotificacaoCommand(Guid TenantId, Guid UsuarioAdminId, string? Ip, string? UserAgent, CriarNotificacaoRequest Request)
    : IRequest<Result<NotificacaoResponse>>;
