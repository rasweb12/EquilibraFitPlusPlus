using EquilibraFitPlusPlus.Application.Features.Evolucao.Commands.SalvarPeso;
using EquilibraFitPlusPlus.Contracts.Evolucao;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquilibraFitPlusPlus.Api.Controllers;

/// <summary>
/// Body evolution endpoints.
/// </summary>
[Authorize]
[Route("api/v1/evolucao")]
public sealed class EvolucaoController : ApiControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes the controller.
    /// </summary>
    public EvolucaoController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Saves or updates a body weight progress log.
    /// </summary>
    [HttpPost("peso")]
    [ProducesResponseType(typeof(RegistroEvolucaoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SalvarPeso([FromBody] SalvarPesoRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new SalvarPesoCommand(tenantId, usuarioId, request), cancellationToken);
        return HandleResult(result);
    }
}
