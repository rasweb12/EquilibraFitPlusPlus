using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Queries.ObterTreinoAtivo;

/// <summary>
/// Query used to get the active workout.
/// </summary>
public sealed record ObterTreinoAtivoQuery(Guid TenantId, Guid UsuarioId) : IRequest<Result<TreinoUsuarioResponse>>;
