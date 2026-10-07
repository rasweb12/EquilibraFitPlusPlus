using EquilibraFitPlusPlus.Application.Features.Premium.Queries.ObterPremiumStatus;
using EquilibraFitPlusPlus.Contracts.Premium;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquilibraFitPlusPlus.Api.Controllers;

/// <summary>
/// Premium entitlement endpoints.
/// </summary>
[Authorize]
[Route("api/v1/premium")]
public sealed class PremiumController : ApiControllerBase
{
    private readonly ISender _sender;

    /// <summary>Initializes the controller.</summary>
    public PremiumController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Gets the current user's premium status.</summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(PremiumStatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterStatus(CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ObterPremiumStatusQuery(tenantId, usuarioId), cancellationToken);
        return HandleResult(result);
    }
}
