using EquilibraFitPlusPlus.Contracts.AiCoach;
using EquilibraFitPlusPlus.Shared.Pagination;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.AiCoach.Queries.ListarCoachSessions;

/// <summary>
/// Query used to list AI Coach sessions.
/// </summary>
public sealed record ListarCoachSessionsQuery(Guid TenantId, Guid UsuarioId, int Page, int PageSize)
    : IRequest<Result<PagedResult<CoachSessionResponse>>>;
