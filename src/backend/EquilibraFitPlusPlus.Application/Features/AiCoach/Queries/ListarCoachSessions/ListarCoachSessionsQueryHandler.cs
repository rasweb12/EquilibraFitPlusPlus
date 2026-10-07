using EquilibraFitPlusPlus.Application.Abstractions.AiCoach;
using EquilibraFitPlusPlus.Application.Features.AiCoach.Mappings;
using EquilibraFitPlusPlus.Contracts.AiCoach;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Pagination;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.AiCoach.Queries.ListarCoachSessions;

/// <summary>
/// Handles AI Coach session listing.
/// </summary>
public sealed class ListarCoachSessionsQueryHandler : IRequestHandler<ListarCoachSessionsQuery, Result<PagedResult<CoachSessionResponse>>>
{
    private readonly IAiCoachRepository _aiCoachRepository;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public ListarCoachSessionsQueryHandler(IAiCoachRepository aiCoachRepository)
    {
        _aiCoachRepository = aiCoachRepository;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<CoachSessionResponse>>> Handle(ListarCoachSessionsQuery request, CancellationToken cancellationToken)
    {
        PagedResult<ChatSession> sessions = await _aiCoachRepository.ListarSessoesAsync(request.TenantId, request.UsuarioId, request.Page, request.PageSize, cancellationToken);
        var response = new PagedResult<CoachSessionResponse>(
            sessions.Items.Select(CoachSessionMapper.Map).ToArray(),
            sessions.Page,
            sessions.PageSize,
            sessions.TotalItems);

        return Result<PagedResult<CoachSessionResponse>>.Success(response);
    }
}
