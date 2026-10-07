using EquilibraFitPlusPlus.Application.Features.Gamificacao.Queries.ObterGamificacaoUsuario;
using EquilibraFitPlusPlus.Contracts.Gamificacao;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquilibraFitPlusPlus.Api.Controllers;

/// <summary>
/// Gamification endpoints.
/// </summary>
[Authorize]
[Route("api/v1/gamificacao")]
public sealed class GamificacaoController : ApiControllerBase
{
    private readonly ISender _sender;

    /// <summary>Initializes the controller.</summary>
    public GamificacaoController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Gets the authenticated user's gamification summary.</summary>
    [HttpGet("resumo")]
    [ProducesResponseType(typeof(GamificacaoResumoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterResumo(CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ObterGamificacaoUsuarioQuery(tenantId, usuarioId), cancellationToken);
        return HandleResult(result);
    }
}
