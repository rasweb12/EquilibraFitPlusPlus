using EquilibraFitPlusPlus.Application.Features.Onboarding.Commands.SalvarQuestionario;
using EquilibraFitPlusPlus.Application.Features.Onboarding.Queries.ObterQuestionario;
using EquilibraFitPlusPlus.Contracts.Onboarding;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquilibraFitPlusPlus.Api.Controllers;

/// <summary>
/// User onboarding endpoints.
/// </summary>
[Authorize]
[Route("api/v1/onboarding")]
public sealed class OnboardingController : ApiControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes the controller.
    /// </summary>
    public OnboardingController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Creates or updates the initial questionnaire.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(QuestionarioResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Salvar([FromBody] SalvarQuestionarioRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new SalvarQuestionarioCommand(tenantId, usuarioId, request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets the current user questionnaire.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(QuestionarioResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterMeuQuestionario(CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ObterQuestionarioQuery(tenantId, usuarioId), cancellationToken);
        return HandleResult(result);
    }
}
