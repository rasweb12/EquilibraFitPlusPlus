using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Shared.Pagination;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Queries.ListarExercicios;

/// <summary>
/// Query used to list exercise catalog items.
/// </summary>
public sealed record ListarExerciciosQuery(Guid TenantId, string? Termo, int Page, int PageSize) : IRequest<Result<PagedResult<ExercicioResponse>>>;
