using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.GerarPropostaEvolucaoTreino;

/// <summary>
/// Command used to generate a workout evolution proposal.
/// </summary>
public sealed record GerarPropostaEvolucaoTreinoCommand(
    Guid TenantId,
    Guid UsuarioId,
    Guid TreinoId,
    GerarPropostaEvolucaoTreinoRequest Request) : IRequest<Result<TreinoEvolucaoPropostaResponse>>;
