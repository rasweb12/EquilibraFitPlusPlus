using EquilibraFitPlusPlus.Contracts.AiCoach;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.AiCoach.Queries.ObterCoachSession;

/// <summary>
/// Query used to get an AI Coach session.
/// </summary>
public sealed record ObterCoachSessionQuery(Guid TenantId, Guid UsuarioId, Guid SessaoId) : IRequest<Result<CoachSessionResponse>>;
