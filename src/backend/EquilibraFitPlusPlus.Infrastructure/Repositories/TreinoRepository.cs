using EquilibraFitPlusPlus.Application.Abstractions.Treinos;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Infrastructure.Data;
using EquilibraFitPlusPlus.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace EquilibraFitPlusPlus.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation for workouts and exercise catalog.
/// </summary>
public sealed class TreinoRepository : ITreinoRepository
{
    private readonly EquilibraFitPlusPlusDbContext _dbContext;

    /// <summary>
    /// Initializes the repository.
    /// </summary>
    public TreinoRepository(EquilibraFitPlusPlusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public void AdicionarTreino(TreinoUsuario treinoUsuario)
    {
        _dbContext.TreinosUsuario.Add(treinoUsuario);
    }

    /// <inheritdoc />
    public void AdicionarPropostaEvolucao(TreinoEvolucaoProposta proposta)
    {
        _dbContext.TreinosEvolucoesPropostas.Add(proposta);
    }

    /// <inheritdoc />
    public void AdicionarExercicio(Exercicio exercicio)
    {
        _dbContext.Exercicios.Add(exercicio);
    }

    /// <inheritdoc />
    public void AdicionarTreinoExercicio(TreinoExercicio treinoExercicio)
    {
        _dbContext.TreinosExercicios.Add(treinoExercicio);
    }

    /// <inheritdoc />
    public void AdicionarSessao(TreinoSessao sessao)
    {
        _dbContext.TreinosSessoes.Add(sessao);
    }

    /// <inheritdoc />
    public Task<TreinoSessao?> ObterSessaoPorOperacaoAsync(Guid tenantId, Guid usuarioId, Guid operationId, CancellationToken cancellationToken)
    {
        return _dbContext.TreinosSessoes
            .Include(sessao => sessao.Series)
            .FirstOrDefaultAsync(
                sessao =>
                    sessao.TenantId == tenantId
                    && sessao.UsuarioId == usuarioId
                    && sessao.OperationId == operationId,
                cancellationToken);
    }

    /// <inheritdoc />
    public void AdicionarProgressaoSugestao(TreinoProgressaoSugestao sugestao)
    {
        _dbContext.TreinosProgressoesSugestoes.Add(sugestao);
    }

    /// <inheritdoc />
    public Task<TreinoUsuario?> ObterTreinoPorIdAsync(Guid tenantId, Guid usuarioId, Guid treinoId, CancellationToken cancellationToken)
    {
        return QueryTreinos()
            .FirstOrDefaultAsync(treino => treino.TenantId == tenantId && treino.UsuarioId == usuarioId && treino.Id == treinoId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TreinoUsuario?> ObterTreinoAtivoAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken)
    {
        return QueryTreinos()
            .OrderByDescending(treino => treino.CriadoEm)
            .FirstOrDefaultAsync(treino => treino.TenantId == tenantId && treino.UsuarioId == usuarioId && treino.Ativo, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> ObterMaiorVersaoAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken)
    {
        return await _dbContext.TreinosUsuario
            .Where(treino => treino.TenantId == tenantId && treino.UsuarioId == usuarioId)
            .Select(treino => (int?)treino.Versao)
            .MaxAsync(cancellationToken) ?? 0;
    }

    /// <inheritdoc />
    public Task<TreinoExercicio?> ObterTreinoExercicioAsync(Guid tenantId, Guid usuarioId, Guid treinoId, Guid treinoExercicioId, CancellationToken cancellationToken)
    {
        return _dbContext.TreinosExercicios
            .Include(item => item.Exercicio)
            .Include(item => item.TreinoUsuario)
            .ThenInclude(treino => treino!.Exercicios)
            .FirstOrDefaultAsync(item =>
                item.TenantId == tenantId
                && item.Id == treinoExercicioId
                && item.TreinoUsuarioId == treinoId
                && item.TreinoUsuario != null
                && item.TreinoUsuario.UsuarioId == usuarioId,
                cancellationToken);
    }

    /// <inheritdoc />
    public Task<TreinoEvolucaoProposta?> ObterPropostaEvolucaoAsync(Guid tenantId, Guid usuarioId, Guid propostaId, CancellationToken cancellationToken)
    {
        return _dbContext.TreinosEvolucoesPropostas
            .Include(proposta => proposta.TreinoUsuario)
            .FirstOrDefaultAsync(proposta =>
                proposta.TenantId == tenantId
                && proposta.UsuarioId == usuarioId
                && proposta.Id == propostaId,
                cancellationToken);
    }

    /// <inheritdoc />
    public Task<TreinoProgressaoSugestao?> ObterProgressaoSugestaoAsync(Guid tenantId, Guid usuarioId, Guid sugestaoId, CancellationToken cancellationToken)
    {
        return _dbContext.TreinosProgressoesSugestoes
            .Include(sugestao => sugestao.TreinoExercicio)
            .ThenInclude(item => item!.Exercicio)
            .FirstOrDefaultAsync(sugestao =>
                sugestao.TenantId == tenantId
                && sugestao.UsuarioId == usuarioId
                && sugestao.Id == sugestaoId,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PagedResult<TreinoUsuario>> ListarTreinosAsync(Guid tenantId, Guid usuarioId, bool? ativo, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<TreinoUsuario> query = QueryTreinos()
            .Where(treino => treino.TenantId == tenantId && treino.UsuarioId == usuarioId);

        if (ativo.HasValue)
        {
            query = query.Where(treino => treino.Ativo == ativo.Value);
        }

        int totalItems = await query.CountAsync(cancellationToken);
        TreinoUsuario[] items = await query
            .OrderByDescending(treino => treino.Ativo)
            .ThenByDescending(treino => treino.CriadoEm)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<TreinoUsuario>(items, page, pageSize, totalItems);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, Exercicio>> ObterExerciciosPorIdsAsync(Guid tenantId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, Exercicio>();
        }

        return await _dbContext.Exercicios
            .Where(exercicio => exercicio.TenantId == tenantId && ids.Contains(exercicio.Id))
            .ToDictionaryAsync(exercicio => exercicio.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PagedResult<Exercicio>> ListarExerciciosAsync(Guid tenantId, string? termo, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<Exercicio> query = _dbContext.Exercicios
            .Where(exercicio => exercicio.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(termo))
        {
            string normalizedTerm = termo.Trim();
            query = query.Where(exercicio =>
                exercicio.Nome.Contains(normalizedTerm)
                || exercicio.GrupoMuscular.Contains(normalizedTerm)
                || exercicio.Nivel.Contains(normalizedTerm));
        }

        int totalItems = await query.CountAsync(cancellationToken);
        Exercicio[] items = await query
            .OrderBy(exercicio => exercicio.GrupoMuscular)
            .ThenBy(exercicio => exercicio.Nome)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<Exercicio>(items, page, pageSize, totalItems);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Exercicio>> ListarSubstitutosAsync(Guid tenantId, Guid exercicioAtualId, string grupoMuscular, string? equipamento, int take, CancellationToken cancellationToken)
    {
        IQueryable<Exercicio> query = _dbContext.Exercicios
            .Where(exercicio =>
                exercicio.TenantId == tenantId
                && exercicio.Id != exercicioAtualId
                && exercicio.GrupoMuscular == grupoMuscular);

        IOrderedQueryable<Exercicio> orderedQuery;
        if (!string.IsNullOrWhiteSpace(equipamento))
        {
            string normalizedEquipment = equipamento.Trim();
            orderedQuery = query.OrderByDescending(exercicio => exercicio.Equipamento != null && exercicio.Equipamento.Contains(normalizedEquipment));
        }
        else
        {
            orderedQuery = query.OrderBy(exercicio => exercicio.Nome);
        }

        return await orderedQuery
            .ThenBy(exercicio => exercicio.Nome)
            .Take(take)
            .ToArrayAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<TreinoExercicioConclusao>> ListarConclusoesAsync(
        Guid tenantId,
        Guid usuarioId,
        Guid treinoId,
        DateOnly data,
        CancellationToken cancellationToken)
    {
        return await _dbContext.TreinosExerciciosConclusoes
            .Where(conclusao =>
                conclusao.TenantId == tenantId
                && conclusao.UsuarioId == usuarioId
                && conclusao.TreinoUsuarioId == treinoId
                && conclusao.Data == data)
            .ToArrayAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<TreinoSerieRealizada>> ListarSeriesPorTreinoAsync(Guid tenantId, Guid usuarioId, Guid treinoId, int take, CancellationToken cancellationToken)
    {
        return await _dbContext.TreinosSeriesRealizadas
            .Include(serie => serie.Sessao)
            .Include(serie => serie.TreinoExercicio)
            .ThenInclude(item => item!.Exercicio)
            .Where(serie =>
                serie.TenantId == tenantId
                && serie.Sessao != null
                && serie.Sessao.UsuarioId == usuarioId
                && serie.Sessao.TreinoUsuarioId == treinoId)
            .OrderByDescending(serie => serie.Sessao!.Data)
            .ThenByDescending(serie => serie.CriadoEm)
            .Take(take)
            .ToArrayAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<TreinoSerieRealizada>> ListarSeriesPorExercicioAsync(Guid tenantId, Guid usuarioId, Guid exercicioId, int take, CancellationToken cancellationToken)
    {
        return await _dbContext.TreinosSeriesRealizadas
            .Include(serie => serie.Sessao)
            .Include(serie => serie.TreinoExercicio)
            .ThenInclude(item => item!.Exercicio)
            .Where(serie =>
                serie.TenantId == tenantId
                && serie.Sessao != null
                && serie.Sessao.UsuarioId == usuarioId
                && serie.TreinoExercicio != null
                && serie.TreinoExercicio.ExercicioId == exercicioId)
            .OrderByDescending(serie => serie.Sessao!.Data)
            .ThenByDescending(serie => serie.CriadoEm)
            .Take(take)
            .ToArrayAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<TreinoExercicioConclusao?> ObterConclusaoAsync(
        Guid tenantId,
        Guid usuarioId,
        Guid treinoExercicioId,
        DateOnly data,
        CancellationToken cancellationToken)
    {
        return _dbContext.TreinosExerciciosConclusoes
            .FirstOrDefaultAsync(
                conclusao =>
                    conclusao.TenantId == tenantId
                    && conclusao.UsuarioId == usuarioId
                    && conclusao.TreinoExercicioId == treinoExercicioId
                    && conclusao.Data == data,
                cancellationToken);
    }

    /// <inheritdoc />
    public void AdicionarConclusao(TreinoExercicioConclusao conclusao)
    {
        _dbContext.TreinosExerciciosConclusoes.Add(conclusao);
    }

    private IQueryable<TreinoUsuario> QueryTreinos()
    {
        return _dbContext.TreinosUsuario
            .Include(treino => treino.Exercicios)
            .ThenInclude(item => item.Exercicio);
    }
}
