using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.SubstituirTreinoExercicio;

/// <summary>
/// Command used to replace one prescribed exercise.
/// </summary>
public sealed record SubstituirTreinoExercicioCommand(
    Guid TenantId,
    Guid UsuarioId,
    Guid TreinoId,
    Guid TreinoExercicioId,
    SubstituirTreinoExercicioRequest Request) : IRequest<Result<TreinoUsuarioResponse>>;
