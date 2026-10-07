using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands
    .AdicionarTreinoExercicio;

public sealed record AdicionarTreinoExercicioCommand(
    Guid TenantId,
    Guid UsuarioId,
    Guid TreinoId,
    AdicionarTreinoExercicioRequest Request)
    : IRequest<Result<TreinoUsuarioResponse>>;
