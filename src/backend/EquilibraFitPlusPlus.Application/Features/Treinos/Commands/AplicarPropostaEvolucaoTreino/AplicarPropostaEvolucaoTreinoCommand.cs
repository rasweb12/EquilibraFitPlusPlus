using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.AplicarPropostaEvolucaoTreino;

/// <summary>
/// Command used to apply a workout evolution proposal.
/// </summary>
public sealed record AplicarPropostaEvolucaoTreinoCommand(
    Guid TenantId,
    Guid UsuarioId,
    Guid TreinoId,
    Guid PropostaId) : IRequest<Result<TreinoUsuarioResponse>>;
