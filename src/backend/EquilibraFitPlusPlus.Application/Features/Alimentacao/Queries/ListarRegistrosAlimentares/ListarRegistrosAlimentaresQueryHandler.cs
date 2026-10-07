using EquilibraFitPlusPlus.Application.Abstractions.Alimentacao;
using EquilibraFitPlusPlus.Application.Features.Alimentacao.Mappings;
using EquilibraFitPlusPlus.Contracts.Alimentacao;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Pagination;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Alimentacao.Queries.ListarRegistrosAlimentares;

/// <summary>
/// Handles food log listing.
/// </summary>
public sealed class ListarRegistrosAlimentaresQueryHandler : IRequestHandler<ListarRegistrosAlimentaresQuery, Result<PagedResult<RegistroAlimentarResponse>>>
{
    private readonly IAlimentacaoRepository _alimentacaoRepository;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public ListarRegistrosAlimentaresQueryHandler(IAlimentacaoRepository alimentacaoRepository)
    {
        _alimentacaoRepository = alimentacaoRepository;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<RegistroAlimentarResponse>>> Handle(ListarRegistrosAlimentaresQuery request, CancellationToken cancellationToken)
    {
        PagedResult<RegistroAlimentar> registros = await _alimentacaoRepository.ListarAsync(
            request.TenantId,
            request.UsuarioId,
            request.Inicio,
            request.Fim,
            request.Page,
            request.PageSize,
            cancellationToken);

        var response = new PagedResult<RegistroAlimentarResponse>(
            registros.Items.Select(RegistroAlimentarMapper.Map).ToArray(),
            registros.Page,
            registros.PageSize,
            registros.TotalItems);

        return Result<PagedResult<RegistroAlimentarResponse>>.Success(response);
    }
}
