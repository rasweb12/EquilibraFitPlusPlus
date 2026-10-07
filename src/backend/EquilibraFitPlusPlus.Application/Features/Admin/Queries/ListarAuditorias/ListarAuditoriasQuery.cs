using EquilibraFitPlusPlus.Contracts.Admin;
using EquilibraFitPlusPlus.Shared.Pagination;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Queries.ListarAuditorias;

/// <summary>
/// Query used to list administrative audit entries.
/// </summary>
public sealed record ListarAuditoriasQuery(Guid TenantId, string? Entidade, int Page, int PageSize)
    : IRequest<Result<PagedResult<AuditoriaResponse>>>;
