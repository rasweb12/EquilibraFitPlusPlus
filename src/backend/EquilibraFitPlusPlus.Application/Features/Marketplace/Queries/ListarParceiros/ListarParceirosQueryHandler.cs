using EquilibraFitPlusPlus.Application.Abstractions.Marketplace;
using EquilibraFitPlusPlus.Contracts.Marketplace;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Pagination;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Marketplace.Queries.ListarParceiros;

/// <summary>
/// Handles partner listing.
/// </summary>
public sealed class ListarParceirosQueryHandler : IRequestHandler<ListarParceirosQuery, Result<PagedResult<ParceiroResponse>>>
{
    private readonly IMarketplaceRepository _marketplaceRepository;

    /// <summary>Initializes the handler.</summary>
    public ListarParceirosQueryHandler(IMarketplaceRepository marketplaceRepository)
    {
        _marketplaceRepository = marketplaceRepository;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<ParceiroResponse>>> Handle(ListarParceirosQuery request, CancellationToken cancellationToken)
    {
        PagedResult<Parceiro> source = await _marketplaceRepository.ListarParceirosAsync(
            request.TenantId,
            request.Termo,
            request.SomenteAprovados,
            request.Page,
            request.PageSize,
            cancellationToken);
        return Result<PagedResult<ParceiroResponse>>.Success(new PagedResult<ParceiroResponse>(
            source.Items.Select(MarketplaceMapper.Map).ToArray(),
            source.Page,
            source.PageSize,
            source.TotalItems));
    }
}
