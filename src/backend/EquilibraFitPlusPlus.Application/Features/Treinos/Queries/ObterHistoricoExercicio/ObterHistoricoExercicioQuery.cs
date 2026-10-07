using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Queries.ObterHistoricoExercicio;

/// <summary>
/// Query used to obtain exercise execution history.
/// </summary>
public sealed record ObterHistoricoExercicioQuery(
    Guid TenantId,
    Guid UsuarioId,
    Guid ExercicioId) : IRequest<Result<HistoricoExercicioResponse>>;
