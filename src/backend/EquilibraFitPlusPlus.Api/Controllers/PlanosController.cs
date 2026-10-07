using EquilibraFitPlusPlus.Application.Features.Planos.Commands.GerarPlanoAlimentar;
using EquilibraFitPlusPlus.Contracts.Planos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquilibraFitPlusPlus.Api.Controllers;

/// <summary>
/// Diet plan endpoints.
/// </summary>
[Authorize]
[Route("api/v1/planos")]
public sealed class PlanosController : ApiControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes the controller.
    /// </summary>
    public PlanosController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Generates and activates a personalized diet plan.
    /// </summary>
    [HttpPost("alimentar/gerar")]
    [ProducesResponseType(typeof(PlanoAlimentarGeradoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GerarPlanoAlimentar([FromBody] GerarPlanoAlimentarRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new GerarPlanoAlimentarCommand(tenantId, usuarioId, request), cancellationToken);
        return HandleResult(result);
    }
}
