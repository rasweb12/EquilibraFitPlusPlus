using EquilibraFitPlusPlus.Application.Abstractions.Onboarding;
using EquilibraFitPlusPlus.Application.Features.Onboarding.Commands.SalvarQuestionario;
using EquilibraFitPlusPlus.Contracts.Onboarding;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Onboarding.Queries.ObterQuestionario;

/// <summary>
/// Handles questionnaire reads.
/// </summary>
public sealed class ObterQuestionarioQueryHandler : IRequestHandler<ObterQuestionarioQuery, Result<QuestionarioResponse>>
{
    private readonly IOnboardingRepository _onboardingRepository;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public ObterQuestionarioQueryHandler(IOnboardingRepository onboardingRepository)
    {
        _onboardingRepository = onboardingRepository;
    }

    /// <inheritdoc />
    public async Task<Result<QuestionarioResponse>> Handle(ObterQuestionarioQuery request, CancellationToken cancellationToken)
    {
        PerfilSaude? perfil = await _onboardingRepository.ObterPorUsuarioAsync(request.TenantId, request.UsuarioId, cancellationToken);
        return perfil is null
            ? Result<QuestionarioResponse>.Failure(new Error("onboarding.nao_encontrado", "Questionário ainda não foi preenchido. Vamos começar quando você estiver pronto."))
            : Result<QuestionarioResponse>.Success(SalvarQuestionarioCommandHandler.Map(perfil));
    }
}
