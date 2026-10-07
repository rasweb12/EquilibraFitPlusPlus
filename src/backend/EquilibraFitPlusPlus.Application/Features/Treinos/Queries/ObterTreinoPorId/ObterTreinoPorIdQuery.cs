using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Queries.ObterTreinoPorId;

/// <summary>
/// Query used to obtain one owned workout plan.
/// </summary>
public sealed record ObterTreinoPorIdQuery(
    Guid TenantId,
    Guid UsuarioId,
    Guid TreinoId) : IRequest<Result<TreinoUsuarioResponse>>;
