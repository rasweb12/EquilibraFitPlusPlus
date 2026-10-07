using EquilibraFitPlusPlus.Application.Features.Lgpd.Commands.SolicitarExclusaoLgpd;
using EquilibraFitPlusPlus.Application.Features.Lgpd.Queries.ExportarDadosUsuario;
using EquilibraFitPlusPlus.Contracts.Lgpd;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquilibraFitPlusPlus.Api.Controllers;

/// <summary>
/// LGPD user rights endpoints.
/// </summary>
[Authorize]
[Route("api/v1/lgpd")]
public sealed class LgpdController : ApiControllerBase
{
    private readonly ISender _sender;

    /// <summary>Initializes the controller.</summary>
    public LgpdController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Exports structured user data.</summary>
    [HttpGet("exportar")]
    [ProducesResponseType(typeof(LgpdExportResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Exportar(CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ExportarDadosUsuarioQuery(tenantId, usuarioId, GetIp(), GetUserAgent()), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Records a deletion request protocol.</summary>
    [HttpPost("solicitar-exclusao")]
    [ProducesResponseType(typeof(SolicitacaoExclusaoLgpdResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SolicitarExclusao([FromBody] SolicitarExclusaoLgpdRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new SolicitarExclusaoLgpdCommand(tenantId, usuarioId, GetIp(), GetUserAgent(), request), cancellationToken);
        return HandleResult(result);
    }

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? GetUserAgent() => Request.Headers["User-Agent"].ToString();
}
