using EquilibraFitPlusPlus.Contracts.Premium;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Premium.Queries.ObterPremiumStatus;

/// <summary>
/// Query used to get premium status for a user.
/// </summary>
public sealed record ObterPremiumStatusQuery(Guid TenantId, Guid UsuarioId) : IRequest<Result<PremiumStatusResponse>>;
