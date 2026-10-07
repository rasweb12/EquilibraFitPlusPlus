using EquilibraFitPlusPlus.Contracts.Premium;
using EquilibraFitPlusPlus.Shared.Pagination;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Premium.Queries.ListarCuponsAdmin;

/// <summary>
/// Query used to list coupons for administrators.
/// </summary>
public sealed record ListarCuponsAdminQuery(Guid TenantId, int Page, int PageSize)
    : IRequest<Result<PagedResult<CupomResponse>>>;
