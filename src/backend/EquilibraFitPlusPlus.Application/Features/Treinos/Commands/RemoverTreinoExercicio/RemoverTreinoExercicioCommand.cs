using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands
    .RemoverTreinoExercicio;

public sealed record RemoverTreinoExercicioCommand(
    Guid TenantId,
    Guid UsuarioId,
    Guid TreinoId,
    Guid TreinoExercicioId)
    : IRequest<Result<TreinoUsuarioResponse>>;
