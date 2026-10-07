using EquilibraFitPlusPlus.Application.Features.Dashboard.Queries.ObterDashboardUsuario;
using EquilibraFitPlusPlus.Contracts.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquilibraFitPlusPlus.Api.Controllers;

/// <summary>
/// User dashboard endpoints.
/// </summary>
[Authorize]
[Route("api/v1/dashboard")]
public sealed class DashboardController : ApiControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes the controller.
    /// </summary>
    public DashboardController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Gets the current user dashboard.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(DashboardUsuarioResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Obter([FromQuery] DateOnly? data, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ObterDashboardUsuarioQuery(tenantId, usuarioId, data ?? DateOnly.FromDateTime(DateTime.UtcNow)), cancellationToken);
        return HandleResult(result);
    }
}
