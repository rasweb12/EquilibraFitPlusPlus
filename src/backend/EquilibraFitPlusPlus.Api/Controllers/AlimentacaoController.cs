using EquilibraFitPlusPlus.Application.Features.Alimentacao.Commands.AtualizarRegistroAlimentar;
using EquilibraFitPlusPlus.Application.Features.Alimentacao.Commands.CriarRegistroAlimentar;
using EquilibraFitPlusPlus.Application.Features.Alimentacao.Commands.EstimarRefeicaoTexto;
using EquilibraFitPlusPlus.Application.Features.Alimentacao.Commands.ExcluirRegistroAlimentar;
using EquilibraFitPlusPlus.Application.Features.Alimentacao.Commands.ReconhecerRefeicaoImagem;
using EquilibraFitPlusPlus.Application.Features.Alimentacao.Commands.ReconhecerRotuloImagem;
using EquilibraFitPlusPlus.Application.Features.Alimentacao.Queries.ListarRegistrosAlimentares;
using EquilibraFitPlusPlus.Application.Features.Alimentacao.Queries.ObterRegistroAlimentar;
using EquilibraFitPlusPlus.Contracts.Alimentacao;
using EquilibraFitPlusPlus.Shared.Pagination;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquilibraFitPlusPlus.Api.Controllers;

/// <summary>
/// Food log endpoints.
/// </summary>
[Authorize]
[Route("api/v1/alimentacao")]
public sealed class AlimentacaoController : ApiControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes the controller.
    /// </summary>
    public AlimentacaoController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Recognizes a meal image and returns editable suggestions.
    /// </summary>
    [HttpPost("reconhecer-refeicao")]
    [ProducesResponseType(typeof(RefeicaoImagemReconhecidaResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReconhecerRefeicao([FromBody] ReconhecerRefeicaoImagemRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ReconhecerRefeicaoImagemCommand(tenantId, usuarioId, request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Recognizes a nutrition label image and returns editable suggestions.
    /// </summary>
    [HttpPost("reconhecer-rotulo")]
    [ProducesResponseType(typeof(RotuloNutricionalReconhecidoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReconhecerRotulo([FromBody] ReconhecerRotuloImagemRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ReconhecerRotuloImagemCommand(tenantId, usuarioId, request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Estimates calories and macros from a textual meal description.
    /// </summary>
    [HttpPost("estimar-texto")]
    [ProducesResponseType(typeof(RefeicaoTextoEstimadaResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> EstimarTexto([FromBody] EstimarRefeicaoTextoRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new EstimarRefeicaoTextoCommand(tenantId, usuarioId, request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Creates a food log.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(RegistroAlimentarResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Criar([FromBody] CriarRegistroAlimentarRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new CriarRegistroAlimentarCommand(tenantId, usuarioId, request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Updates a food log.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(RegistroAlimentarResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarRegistroAlimentarRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new AtualizarRegistroAlimentarCommand(tenantId, usuarioId, id, request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Lists food logs.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<RegistroAlimentarResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] DateOnly? inicio, [FromQuery] DateOnly? fim, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ListarRegistrosAlimentaresQuery(tenantId, usuarioId, inicio, fim, page, pageSize), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets a food log by identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RegistroAlimentarResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Obter(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ObterRegistroAlimentarQuery(tenantId, usuarioId, id), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Soft deletes a food log.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ExcluirRegistroAlimentarCommand(tenantId, usuarioId, id), cancellationToken);
        return HandleResult(result);
    }
}
