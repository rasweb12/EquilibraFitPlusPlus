using EquilibraFitPlusPlus.Contracts.Onboarding;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Onboarding.Commands.SalvarQuestionario;

/// <summary>
/// Command used to save the initial questionnaire.
/// </summary>
public sealed record SalvarQuestionarioCommand(Guid TenantId, Guid UsuarioId, SalvarQuestionarioRequest Request)
    : IRequest<Result<QuestionarioResponse>>;
