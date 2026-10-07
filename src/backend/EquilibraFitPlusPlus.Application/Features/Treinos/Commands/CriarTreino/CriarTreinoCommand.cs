using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.CriarTreino;

/// <summary>
/// Command used to create a user workout.
/// </summary>
public sealed record CriarTreinoCommand(Guid TenantId, Guid UsuarioId, CriarTreinoRequest Request)
    : IRequest<Result<TreinoUsuarioResponse>>;
