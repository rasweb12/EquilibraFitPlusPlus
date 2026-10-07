using EquilibraFitPlusPlus.Application.Features.Notificacoes.Commands.MarcarNotificacaoLida;
using EquilibraFitPlusPlus.Application.Features.Notificacoes.Queries.ListarNotificacoes;
using EquilibraFitPlusPlus.Contracts.Notificacoes;
using EquilibraFitPlusPlus.Shared.Pagination;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquilibraFitPlusPlus.Api.Controllers;

/// <summary>
/// Notification endpoints.
/// </summary>
[Authorize]
[Route("api/v1/notificacoes")]
public sealed class NotificacoesController : ApiControllerBase
{
    private readonly ISender _sender;

    /// <summary>Initializes the controller.</summary>
    public NotificacoesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Lists notifications for the authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NotificacaoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ListarNotificacoesQuery(tenantId, usuarioId, page, pageSize), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Marks a notification as read.</summary>
    [HttpPut("{id:guid}/lida")]
    [ProducesResponseType(typeof(NotificacaoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarcarLida(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new MarcarNotificacaoLidaCommand(tenantId, usuarioId, id), cancellationToken);
        return HandleResult(result);
    }
}
