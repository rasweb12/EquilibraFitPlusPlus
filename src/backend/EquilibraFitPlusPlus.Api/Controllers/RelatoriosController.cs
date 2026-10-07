using EquilibraFitPlusPlus.Application.Features.Relatorios.Queries.ObterRelatorioUsuario;
using EquilibraFitPlusPlus.Contracts.Relatorios;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquilibraFitPlusPlus.Api.Controllers;

/// <summary>
/// User report endpoints.
/// </summary>
[Authorize]
[Route("api/v1/relatorios")]
public sealed class RelatoriosController : ApiControllerBase
{
    private readonly ISender _sender;

    /// <summary>Initializes the controller.</summary>
    public RelatoriosController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Gets the authenticated user's report summary.</summary>
    [HttpGet("resumo")]
    [ProducesResponseType(typeof(RelatorioUsuarioResumoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterResumo([FromQuery] DateOnly? inicio, [FromQuery] DateOnly? fim, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        DateOnly end = fim ?? DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly start = inicio ?? end.AddDays(-29);
        var result = await _sender.Send(new ObterRelatorioUsuarioQuery(tenantId, usuarioId, start, end), cancellationToken);
        return HandleResult(result);
    }
}
