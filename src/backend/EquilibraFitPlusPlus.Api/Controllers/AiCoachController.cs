using EquilibraFitPlusPlus.Application.Features.AiCoach.Commands.EnviarMensagemCoach;
using EquilibraFitPlusPlus.Application.Features.AiCoach.Queries.ListarCoachSessions;
using EquilibraFitPlusPlus.Application.Features.AiCoach.Queries.ObterCoachSession;
using EquilibraFitPlusPlus.Contracts.AiCoach;
using EquilibraFitPlusPlus.Shared.Pagination;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquilibraFitPlusPlus.Api.Controllers;

/// <summary>
/// AI Coach endpoints.
/// </summary>
[Authorize]
[Route("api/v1/ia/coach")]
public sealed class AiCoachController : ApiControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes the controller.
    /// </summary>
    public AiCoachController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Sends a message to the AI Coach and stores the conversation.
    /// </summary>
    [HttpPost("mensagens")]
    [ProducesResponseType(typeof(CoachReplyResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> EnviarMensagem([FromBody] EnviarMensagemCoachRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new EnviarMensagemCoachCommand(tenantId, usuarioId, request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Lists AI Coach sessions for the current user.
    /// </summary>
    [HttpGet("sessoes")]
    [ProducesResponseType(typeof(PagedResult<CoachSessionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarSessoes([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ListarCoachSessionsQuery(tenantId, usuarioId, page, pageSize), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets an AI Coach session by identifier.
    /// </summary>
    [HttpGet("sessoes/{id:guid}")]
    [ProducesResponseType(typeof(CoachSessionResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterSessao(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ObterCoachSessionQuery(tenantId, usuarioId, id), cancellationToken);
        return HandleResult(result);
    }
}
