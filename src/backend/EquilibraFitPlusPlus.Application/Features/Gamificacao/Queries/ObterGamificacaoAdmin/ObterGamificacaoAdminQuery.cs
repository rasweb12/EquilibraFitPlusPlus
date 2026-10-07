using EquilibraFitPlusPlus.Contracts.Gamificacao;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Gamificacao.Queries.ObterGamificacaoAdmin;

/// <summary>
/// Query used to get administrative gamification summary.
/// </summary>
public sealed record ObterGamificacaoAdminQuery(Guid TenantId) : IRequest<Result<GamificacaoAdminResumoResponse>>;
