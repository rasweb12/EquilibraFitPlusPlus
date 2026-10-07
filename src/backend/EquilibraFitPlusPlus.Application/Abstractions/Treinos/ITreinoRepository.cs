using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Pagination;

namespace EquilibraFitPlusPlus.Application.Abstractions.Treinos;

/// <summary>
/// Provides persistence operations for workouts and exercise catalog.
/// </summary>
public interface ITreinoRepository
{
    /// <summary>
    /// Adds a workout.
    /// </summary>
    void AdicionarTreino(TreinoUsuario treinoUsuario);

    /// <summary>
    /// Adds a workout evolution proposal.
    /// </summary>
    void AdicionarPropostaEvolucao(TreinoEvolucaoProposta proposta);

    /// <summary>
    /// Adds an exercise catalog item.
    /// </summary>
    void AdicionarExercicio(Exercicio exercicio);

    /// <summary>
    /// Adds a prescribed exercise to a workout without updating the loaded workout aggregate.
    /// </summary>
    void AdicionarTreinoExercicio(TreinoExercicio treinoExercicio);

    /// <summary>
    /// Adds a real workout session.
    /// </summary>
    void AdicionarSessao(TreinoSessao sessao);

    /// <summary>
    /// Gets a real workout session previously created by a client operation.
    /// </summary>
    Task<TreinoSessao?> ObterSessaoPorOperacaoAsync(Guid tenantId, Guid usuarioId, Guid operationId, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a progression suggestion.
    /// </summary>
    void AdicionarProgressaoSugestao(TreinoProgressaoSugestao sugestao);

    /// <summary>
    /// Gets a workout by owner and identifier.
    /// </summary>
    Task<TreinoUsuario?> ObterTreinoPorIdAsync(Guid tenantId, Guid usuarioId, Guid treinoId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the active workout for a user.
    /// </summary>
    Task<TreinoUsuario?> ObterTreinoAtivoAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the highest workout version for a user.
    /// </summary>
    Task<int> ObterMaiorVersaoAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a prescribed exercise from an owned workout.
    /// </summary>
    Task<TreinoExercicio?> ObterTreinoExercicioAsync(Guid tenantId, Guid usuarioId, Guid treinoId, Guid treinoExercicioId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a workout evolution proposal by owner and identifier.
    /// </summary>
    Task<TreinoEvolucaoProposta?> ObterPropostaEvolucaoAsync(Guid tenantId, Guid usuarioId, Guid propostaId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a progression suggestion by owner and identifier.
    /// </summary>
    Task<TreinoProgressaoSugestao?> ObterProgressaoSugestaoAsync(Guid tenantId, Guid usuarioId, Guid sugestaoId, CancellationToken cancellationToken);

    /// <summary>
    /// Lists workouts for a user.
    /// </summary>
    Task<PagedResult<TreinoUsuario>> ListarTreinosAsync(Guid tenantId, Guid usuarioId, bool? ativo, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Gets exercise catalog items by identifiers.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, Exercicio>> ObterExerciciosPorIdsAsync(Guid tenantId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    /// <summary>
    /// Lists exercise catalog items.
    /// </summary>
    Task<PagedResult<Exercicio>> ListarExerciciosAsync(Guid tenantId, string? termo, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Finds possible replacement exercises preserving muscle group when possible.
    /// </summary>
    Task<IReadOnlyCollection<Exercicio>> ListarSubstitutosAsync(Guid tenantId, Guid exercicioAtualId, string grupoMuscular, string? equipamento, int take, CancellationToken cancellationToken);

    /// <summary>
    /// Lists completion logs for a workout and date.
    /// </summary>
    Task<IReadOnlyCollection<TreinoExercicioConclusao>> ListarConclusoesAsync(
        Guid tenantId,
        Guid usuarioId,
        Guid treinoId,
        DateOnly data,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lists recent performed sets for a workout.
    /// </summary>
    Task<IReadOnlyCollection<TreinoSerieRealizada>> ListarSeriesPorTreinoAsync(Guid tenantId, Guid usuarioId, Guid treinoId, int take, CancellationToken cancellationToken);

    /// <summary>
    /// Lists recent performed sets for an exercise catalog item.
    /// </summary>
    Task<IReadOnlyCollection<TreinoSerieRealizada>> ListarSeriesPorExercicioAsync(Guid tenantId, Guid usuarioId, Guid exercicioId, int take, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a completion log for a prescribed exercise and date.
    /// </summary>
    Task<TreinoExercicioConclusao?> ObterConclusaoAsync(
        Guid tenantId,
        Guid usuarioId,
        Guid treinoExercicioId,
        DateOnly data,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds a workout exercise completion log.
    /// </summary>
    void AdicionarConclusao(TreinoExercicioConclusao conclusao);
}
