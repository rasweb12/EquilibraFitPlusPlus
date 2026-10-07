using EquilibraFitPlusPlus.Contracts.Onboarding;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Onboarding.Queries.ObterQuestionario;

/// <summary>
/// Query used to get the current user questionnaire.
/// </summary>
public sealed record ObterQuestionarioQuery(Guid TenantId, Guid UsuarioId) : IRequest<Result<QuestionarioResponse>>;
