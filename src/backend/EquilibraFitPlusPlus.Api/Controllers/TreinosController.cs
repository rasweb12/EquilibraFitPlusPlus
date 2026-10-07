using EquilibraFitPlusPlus.Application.Features.Treinos.Commands.AdicionarTreinoExercicio;
using EquilibraFitPlusPlus.Application.Features.Treinos.Commands.AplicarPropostaEvolucaoTreino;
using EquilibraFitPlusPlus.Application.Features.Treinos.Commands.AtualizarTreino;
using EquilibraFitPlusPlus.Application.Features.Treinos.Commands.AtualizarTreinoExercicio;
using EquilibraFitPlusPlus.Application.Features.Treinos.Commands.ConcluirTreinoExercicio;
using EquilibraFitPlusPlus.Application.Features.Treinos.Commands.CriarTreino;
using EquilibraFitPlusPlus.Application.Features.Treinos.Commands.DecidirProgressaoTreino;
using EquilibraFitPlusPlus.Application.Features.Treinos.Commands.ExcluirTreino;
using EquilibraFitPlusPlus.Application.Features.Treinos.Commands.GerarPropostaEvolucaoTreino;
using EquilibraFitPlusPlus.Application.Features.Treinos.Commands.GerarTreinoIa;
using EquilibraFitPlusPlus.Application.Features.Treinos.Commands.RegistrarSessaoTreino;
using EquilibraFitPlusPlus.Application.Features.Treinos.Commands.RemoverTreinoExercicio;
using EquilibraFitPlusPlus.Application.Features.Treinos.Commands.SubstituirTreinoExercicio;
using EquilibraFitPlusPlus.Application.Features.Treinos.Commands.SugerirProgressaoTreino;
using EquilibraFitPlusPlus.Application.Features.Treinos.Queries.ListarExercicios;
using EquilibraFitPlusPlus.Application.Features.Treinos.Queries.ListarTreinos;
using EquilibraFitPlusPlus.Application.Features.Treinos.Queries.ObterHistoricoExercicio;
using EquilibraFitPlusPlus.Application.Features.Treinos.Queries.ObterTreinoAtivo;
using EquilibraFitPlusPlus.Application.Features.Treinos.Queries.ObterTreinoPorId;
using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Shared.Pagination;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquilibraFitPlusPlus.Api.Controllers;

/// <summary>
/// Workout endpoints.
/// </summary>
[Authorize]
[Route("api/v1/treinos")]
public sealed class TreinosController : ApiControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes the controller.
    /// </summary>
    public TreinosController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Generates and activates a workout through AI or the hybrid engine.
    /// </summary>
    [HttpPost("gerar-ia")]
    [ProducesResponseType(typeof(TreinoIaGeradoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GerarIa([FromBody] GerarTreinoIaRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new GerarTreinoIaCommand(tenantId, usuarioId, request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Creates a workout and marks it as the active workout.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TreinoUsuarioResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Criar([FromBody] CriarTreinoRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new CriarTreinoCommand(tenantId, usuarioId, request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Lists user workouts.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TreinoUsuarioResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] bool? ativo, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ListarTreinosQuery(tenantId, usuarioId, ativo, page, pageSize), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets the current active workout.
    /// </summary>
    [HttpGet("ativo")]
    [ProducesResponseType(typeof(TreinoUsuarioResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterAtivo(CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ObterTreinoAtivoQuery(tenantId, usuarioId), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets one user workout, including historical inactive plans.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TreinoUsuarioResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ObterTreinoPorIdQuery(tenantId, usuarioId, id), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Updates workout metadata.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TreinoUsuarioResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarTreinoRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new AtualizarTreinoCommand(tenantId, usuarioId, id, request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Adds one prescribed exercise to an existing workout.
    /// </summary>
    [HttpPost("{treinoId:guid}/exercicios")]
    [ProducesResponseType(
        typeof(TreinoUsuarioResponse),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> AdicionarExercicio(
        Guid treinoId,
        [FromBody] AdicionarTreinoExercicioRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(
                out Guid tenantId,
                out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(
            new AdicionarTreinoExercicioCommand(
                tenantId,
                usuarioId,
                treinoId,
                request),
            cancellationToken);

        return HandleResult(result);
    }

    /// <summary>
    /// Removes one prescribed exercise from an existing workout.
    /// Historical workout sessions must remain preserved.
    /// </summary>
    [HttpDelete(
        "{treinoId:guid}/exercicios/{treinoExercicioId:guid}")]
    [ProducesResponseType(
        typeof(TreinoUsuarioResponse),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoverExercicio(
        Guid treinoId,
        Guid treinoExercicioId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(
                out Guid tenantId,
                out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(
            new RemoverTreinoExercicioCommand(
                tenantId,
                usuarioId,
                treinoId,
                treinoExercicioId),
            cancellationToken);

        return HandleResult(result);
    }

    /// <summary>
    /// Marks a prescribed exercise as completed or pending for a date.
    /// </summary>
    [HttpPut("{treinoId:guid}/exercicios/{treinoExercicioId:guid}/conclusao")]
    [ProducesResponseType(typeof(TreinoExercicioConclusaoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConcluirExercicio(
        Guid treinoId,
        Guid treinoExercicioId,
        [FromBody] ConcluirTreinoExercicioRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ConcluirTreinoExercicioCommand(tenantId, usuarioId, treinoId, treinoExercicioId, request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Updates one prescribed workout exercise.
    /// </summary>
    [HttpPut("{treinoId:guid}/exercicios/{treinoExercicioId:guid}")]
    [ProducesResponseType(typeof(TreinoUsuarioResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AtualizarExercicio(
        Guid treinoId,
        Guid treinoExercicioId,
        [FromBody] AtualizarTreinoExercicioRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new AtualizarTreinoExercicioCommand(tenantId, usuarioId, treinoId, treinoExercicioId, request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Replaces one prescribed workout exercise.
    /// </summary>
    [HttpPost("{treinoId:guid}/exercicios/{treinoExercicioId:guid}/substituir")]
    [ProducesResponseType(typeof(TreinoUsuarioResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubstituirExercicio(
        Guid treinoId,
        Guid treinoExercicioId,
        [FromBody] SubstituirTreinoExercicioRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new SubstituirTreinoExercicioCommand(tenantId, usuarioId, treinoId, treinoExercicioId, request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Registers a real workout execution session.
    /// </summary>
    [HttpPost("{treinoId:guid}/sessoes")]
    [ProducesResponseType(typeof(TreinoSessaoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RegistrarSessao(
        Guid treinoId,
        [FromBody] RegistrarSessaoTreinoRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new RegistrarSessaoTreinoCommand(tenantId, usuarioId, treinoId, request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Generates a proposal to evolve the workout without applying it automatically.
    /// </summary>
    [HttpPost("{treinoId:guid}/evolucoes")]
    [ProducesResponseType(typeof(TreinoEvolucaoPropostaResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GerarPropostaEvolucao(
        Guid treinoId,
        [FromBody] GerarPropostaEvolucaoTreinoRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new GerarPropostaEvolucaoTreinoCommand(tenantId, usuarioId, treinoId, request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Applies a previously generated workout evolution proposal.
    /// </summary>
    [HttpPost("{treinoId:guid}/evolucoes/{propostaId:guid}/aplicar")]
    [ProducesResponseType(typeof(TreinoUsuarioResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AplicarPropostaEvolucao(
        Guid treinoId,
        Guid propostaId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new AplicarPropostaEvolucaoTreinoCommand(tenantId, usuarioId, treinoId, propostaId), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Generates progression suggestions based on execution history.
    /// </summary>
    [HttpPost("{treinoId:guid}/progressoes/sugerir")]
    [ProducesResponseType(typeof(IReadOnlyCollection<TreinoProgressaoSugestaoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SugerirProgressoes(Guid treinoId, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new SugerirProgressaoTreinoCommand(tenantId, usuarioId, treinoId), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Applies or keeps a progression suggestion.
    /// </summary>
    [HttpPost("progressoes/{sugestaoId:guid}/decidir")]
    [ProducesResponseType(typeof(TreinoProgressaoSugestaoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DecidirProgressao(
        Guid sugestaoId,
        [FromBody] DecidirProgressaoTreinoRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new DecidirProgressaoTreinoCommand(tenantId, usuarioId, sugestaoId, request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets execution history for an exercise catalog item.
    /// </summary>
    [HttpGet("exercicios/{exercicioId:guid}/historico")]
    [ProducesResponseType(typeof(HistoricoExercicioResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterHistoricoExercicio(Guid exercicioId, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ObterHistoricoExercicioQuery(tenantId, usuarioId, exercicioId), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Lists exercise catalog items.
    /// </summary>
    [HttpGet("exercicios")]
    [ProducesResponseType(typeof(PagedResult<ExercicioResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarExercicios([FromQuery] string? termo, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserContext(out Guid tenantId, out _))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ListarExerciciosQuery(tenantId, termo, page, pageSize), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Soft deletes a workout.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ExcluirTreinoCommand(tenantId, usuarioId, id), cancellationToken);
        return HandleResult(result);
    }
}
