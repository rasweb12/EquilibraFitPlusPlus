using EquilibraFitPlusPlus.Contracts.Marketplace;
using EquilibraFitPlusPlus.Shared.Pagination;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Marketplace.Queries.ListarParceiros;

/// <summary>
/// Query used to list marketplace partners.
/// </summary>
public sealed record ListarParceirosQuery(Guid TenantId, string? Termo, bool SomenteAprovados, int Page, int PageSize)
    : IRequest<Result<PagedResult<ParceiroResponse>>>;
