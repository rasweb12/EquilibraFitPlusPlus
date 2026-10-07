using EquilibraFitPlusPlus.Application.Abstractions.AiCoach;
using EquilibraFitPlusPlus.Application.Features.AiCoach.Mappings;
using EquilibraFitPlusPlus.Contracts.AiCoach;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.AiCoach.Queries.ObterCoachSession;

/// <summary>
/// Handles AI Coach session detail reads.
/// </summary>
public sealed class ObterCoachSessionQueryHandler : IRequestHandler<ObterCoachSessionQuery, Result<CoachSessionResponse>>
{
    private readonly IAiCoachRepository _aiCoachRepository;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public ObterCoachSessionQueryHandler(IAiCoachRepository aiCoachRepository)
    {
        _aiCoachRepository = aiCoachRepository;
    }

    /// <inheritdoc />
    public async Task<Result<CoachSessionResponse>> Handle(ObterCoachSessionQuery request, CancellationToken cancellationToken)
    {
        ChatSession? session = await _aiCoachRepository.ObterSessaoAsync(request.TenantId, request.UsuarioId, request.SessaoId, cancellationToken);
        return session is null
            ? Result<CoachSessionResponse>.Failure(new Error("ia.sessao_nao_encontrada", "Não encontramos essa conversa do Coach IA."))
            : Result<CoachSessionResponse>.Success(CoachSessionMapper.Map(session));
    }
}
