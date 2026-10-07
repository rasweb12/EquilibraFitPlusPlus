using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.SugerirProgressaoTreino;

/// <summary>
/// Command used to create progression suggestions for a workout.
/// </summary>
public sealed record SugerirProgressaoTreinoCommand(
    Guid TenantId,
    Guid UsuarioId,
    Guid TreinoId) : IRequest<Result<IReadOnlyCollection<TreinoProgressaoSugestaoResponse>>>;
