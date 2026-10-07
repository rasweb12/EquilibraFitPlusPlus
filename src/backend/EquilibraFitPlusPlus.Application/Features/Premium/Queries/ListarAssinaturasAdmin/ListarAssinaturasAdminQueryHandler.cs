using EquilibraFitPlusPlus.Application.Abstractions.Premium;
using EquilibraFitPlusPlus.Contracts.Premium;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Pagination;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Premium.Queries.ListarAssinaturasAdmin;

/// <summary>
/// Handles administrative subscription listing.
/// </summary>
public sealed class ListarAssinaturasAdminQueryHandler : IRequestHandler<ListarAssinaturasAdminQuery, Result<PagedResult<AssinaturaResumoResponse>>>
{
    private readonly IPremiumRepository _premiumRepository;

    /// <summary>Initializes the handler.</summary>
    public ListarAssinaturasAdminQueryHandler(IPremiumRepository premiumRepository)
    {
        _premiumRepository = premiumRepository;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<AssinaturaResumoResponse>>> Handle(ListarAssinaturasAdminQuery request, CancellationToken cancellationToken)
    {
        PagedResult<Assinatura> source = await _premiumRepository.ListarAssinaturasAsync(request.TenantId, request.Page, request.PageSize, cancellationToken);
        return Result<PagedResult<AssinaturaResumoResponse>>.Success(new PagedResult<AssinaturaResumoResponse>(
            source.Items.Select(PremiumMapper.Map).ToArray(),
            source.Page,
            source.PageSize,
            source.TotalItems));
    }
}
