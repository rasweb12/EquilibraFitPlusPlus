using EquilibraFitPlusPlus.Contracts.Premium;
using EquilibraFitPlusPlus.Shared.Pagination;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Premium.Queries.ListarAssinaturasAdmin;

/// <summary>
/// Query used to list subscriptions for administrators.
/// </summary>
public sealed record ListarAssinaturasAdminQuery(Guid TenantId, int Page, int PageSize)
    : IRequest<Result<PagedResult<AssinaturaResumoResponse>>>;
