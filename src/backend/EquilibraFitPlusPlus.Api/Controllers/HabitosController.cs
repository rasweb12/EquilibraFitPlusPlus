using EquilibraFitPlusPlus.Application.Features.Habitos.Commands.SalvarHabitosDiarios;
using EquilibraFitPlusPlus.Application.Features.Habitos.Queries.ObterHabitosDiarios;
using EquilibraFitPlusPlus.Contracts.Habitos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquilibraFitPlusPlus.Api.Controllers;

/// <summary>
/// User daily habits endpoints.
/// </summary>
[Authorize]
[Route("api/v1/habitos")]
public sealed class HabitosController : ApiControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes the controller.
    /// </summary>
    public HabitosController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Gets daily habits for a date.
    /// </summary>
    [HttpGet("diario")]
    [ProducesResponseType(typeof(HabitosDiariosResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterDiario([FromQuery] DateOnly? data, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        DateOnly dia = data ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await _sender.Send(new ObterHabitosDiariosQuery(tenantId, usuarioId, dia), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Saves or updates daily habits.
    /// </summary>
    [HttpPut("diario")]
    [ProducesResponseType(typeof(HabitosDiariosResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SalvarDiario([FromBody] SalvarHabitosDiariosRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new SalvarHabitosDiariosCommand(tenantId, usuarioId, request), cancellationToken);
        return HandleResult(result);
    }
}
