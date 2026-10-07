using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.AtualizarTreinoExercicio;

/// <summary>
/// Command used to update one prescribed workout exercise.
/// </summary>
public sealed record AtualizarTreinoExercicioCommand(
    Guid TenantId,
    Guid UsuarioId,
    Guid TreinoId,
    Guid TreinoExercicioId,
    AtualizarTreinoExercicioRequest Request) : IRequest<Result<TreinoUsuarioResponse>>;
