using EquilibraFitPlusPlus.Application.Features.Marketplace.Queries.ListarParceiros;
using EquilibraFitPlusPlus.Contracts.Marketplace;
using EquilibraFitPlusPlus.Shared.Pagination;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquilibraFitPlusPlus.Api.Controllers;

/// <summary>
/// Marketplace endpoints.
/// </summary>
[Authorize]
[Route("api/v1/marketplace")]
public sealed class MarketplaceController : ApiControllerBase
{
    private readonly ISender _sender;

    /// <summary>Initializes the controller.</summary>
    public MarketplaceController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Lists approved marketplace partners.</summary>
    [HttpGet("parceiros")]
    [ProducesResponseType(typeof(PagedResult<ParceiroResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarParceiros([FromQuery] string? termo, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserContext(out Guid tenantId, out _))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ListarParceirosQuery(tenantId, termo, true, page, pageSize), cancellationToken);
        return HandleResult(result);
    }
}
