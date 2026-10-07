using EquilibraFitPlusPlus.Application.Abstractions.Notificacoes;
using EquilibraFitPlusPlus.Contracts.Notificacoes;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Pagination;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Notificacoes.Queries.ListarNotificacoes;

/// <summary>
/// Handles notification listing.
/// </summary>
public sealed class ListarNotificacoesQueryHandler : IRequestHandler<ListarNotificacoesQuery, Result<PagedResult<NotificacaoResponse>>>
{
    private readonly INotificacaoRepository _notificacaoRepository;

    /// <summary>Initializes the handler.</summary>
    public ListarNotificacoesQueryHandler(INotificacaoRepository notificacaoRepository)
    {
        _notificacaoRepository = notificacaoRepository;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<NotificacaoResponse>>> Handle(ListarNotificacoesQuery request, CancellationToken cancellationToken)
    {
        PagedResult<Notificacao> source = request.UsuarioId.HasValue
            ? await _notificacaoRepository.ListarUsuarioAsync(request.TenantId, request.UsuarioId.Value, request.Page, request.PageSize, cancellationToken)
            : await _notificacaoRepository.ListarAdminAsync(request.TenantId, request.Page, request.PageSize, cancellationToken);

        return Result<PagedResult<NotificacaoResponse>>.Success(new PagedResult<NotificacaoResponse>(
            source.Items.Select(NotificacaoMapper.Map).ToArray(),
            source.Page,
            source.PageSize,
            source.TotalItems));
    }
}
