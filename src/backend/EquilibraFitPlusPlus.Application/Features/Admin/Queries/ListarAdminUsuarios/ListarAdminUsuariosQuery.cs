using EquilibraFitPlusPlus.Contracts.Admin;
using EquilibraFitPlusPlus.Shared.Pagination;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Queries.ListarAdminUsuarios;

/// <summary>
/// Query used to list users in the administrative area.
/// </summary>
public sealed record ListarAdminUsuariosQuery(Guid TenantId, string? Termo, int Page, int PageSize)
    : IRequest<Result<PagedResult<AdminUsuarioResumoResponse>>>;
