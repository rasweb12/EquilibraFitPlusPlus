using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Shared.Pagination;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Queries.ListarTreinos;

/// <summary>
/// Query used to list user workouts.
/// </summary>
public sealed record ListarTreinosQuery(Guid TenantId, Guid UsuarioId, bool? Ativo, int Page, int PageSize)
    : IRequest<Result<PagedResult<TreinoUsuarioResponse>>>;
