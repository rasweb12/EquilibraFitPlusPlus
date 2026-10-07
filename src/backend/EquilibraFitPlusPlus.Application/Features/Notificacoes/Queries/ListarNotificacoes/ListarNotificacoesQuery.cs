using EquilibraFitPlusPlus.Contracts.Notificacoes;
using EquilibraFitPlusPlus.Shared.Pagination;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Notificacoes.Queries.ListarNotificacoes;

/// <summary>
/// Query used to list notifications.
/// </summary>
public sealed record ListarNotificacoesQuery(Guid TenantId, Guid? UsuarioId, int Page, int PageSize)
    : IRequest<Result<PagedResult<NotificacaoResponse>>>;
