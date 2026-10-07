using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.GerarTreinoIa;

/// <summary>
/// Command used to generate a workout with AI or the hybrid engine.
/// </summary>
public sealed record GerarTreinoIaCommand(Guid TenantId, Guid UsuarioId, GerarTreinoIaRequest Request)
    : IRequest<Result<TreinoIaGeradoResponse>>;
