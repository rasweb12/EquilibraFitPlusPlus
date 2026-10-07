using EquilibraFitPlusPlus.Contracts.Gamificacao;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Gamificacao.Queries.ObterGamificacaoUsuario;

/// <summary>
/// Query used to get user gamification summary.
/// </summary>
public sealed record ObterGamificacaoUsuarioQuery(Guid TenantId, Guid UsuarioId) : IRequest<Result<GamificacaoResumoResponse>>;
