using EquilibraFitPlusPlus.Application.Abstractions.Premium;
using EquilibraFitPlusPlus.Contracts.Premium;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Pagination;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Premium.Queries.ListarCuponsAdmin;

/// <summary>
/// Handles administrative coupon listing.
/// </summary>
public sealed class ListarCuponsAdminQueryHandler : IRequestHandler<ListarCuponsAdminQuery, Result<PagedResult<CupomResponse>>>
{
    private readonly IPremiumRepository _premiumRepository;

    /// <summary>Initializes the handler.</summary>
    public ListarCuponsAdminQueryHandler(IPremiumRepository premiumRepository)
    {
        _premiumRepository = premiumRepository;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<CupomResponse>>> Handle(ListarCuponsAdminQuery request, CancellationToken cancellationToken)
    {
        PagedResult<Cupom> source = await _premiumRepository.ListarCuponsAsync(request.TenantId, request.Page, request.PageSize, cancellationToken);
        return Result<PagedResult<CupomResponse>>.Success(new PagedResult<CupomResponse>(
            source.Items.Select(PremiumMapper.Map).ToArray(),
            source.Page,
            source.PageSize,
            source.TotalItems));
    }
}
