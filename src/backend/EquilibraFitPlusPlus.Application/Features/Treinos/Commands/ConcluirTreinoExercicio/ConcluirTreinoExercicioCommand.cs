using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.ConcluirTreinoExercicio;

/// <summary>
/// Command used to update a prescribed workout exercise completion status.
/// </summary>
public sealed record ConcluirTreinoExercicioCommand(
    Guid TenantId,
    Guid UsuarioId,
    Guid TreinoId,
    Guid TreinoExercicioId,
    ConcluirTreinoExercicioRequest Request) : IRequest<Result<TreinoExercicioConclusaoResponse>>;
