using EquilibraFitPlusPlus.Application.Abstractions.Treinos;
using EquilibraFitPlusPlus.Application.Features.Treinos.Mappings;
using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Pagination;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Queries.ListarTreinos;

/// <summary>
/// Handles workout listing.
/// </summary>
public sealed class ListarTreinosQueryHandler : IRequestHandler<ListarTreinosQuery, Result<PagedResult<TreinoUsuarioResponse>>>
{
    private readonly ITreinoRepository _treinoRepository;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public ListarTreinosQueryHandler(ITreinoRepository treinoRepository)
    {
        _treinoRepository = treinoRepository;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<TreinoUsuarioResponse>>> Handle(ListarTreinosQuery request, CancellationToken cancellationToken)
    {
        PagedResult<TreinoUsuario> treinos = await _treinoRepository.ListarTreinosAsync(
            request.TenantId,
            request.UsuarioId,
            request.Ativo,
            request.Page,
            request.PageSize,
            cancellationToken);

        var response = new PagedResult<TreinoUsuarioResponse>(
            treinos.Items.Select(treino => TreinoMapper.Map(treino)).ToArray(),
            treinos.Page,
            treinos.PageSize,
            treinos.TotalItems);

        return Result<PagedResult<TreinoUsuarioResponse>>.Success(response);
    }
}
