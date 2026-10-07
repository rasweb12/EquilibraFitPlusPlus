using EquilibraFitPlusPlus.Application.Abstractions.AiCoach;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Domain.Enums;
using EquilibraFitPlusPlus.Infrastructure.Data;
using EquilibraFitPlusPlus.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace EquilibraFitPlusPlus.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation for AI Coach persistence.
/// </summary>
public sealed class AiCoachRepository : IAiCoachRepository
{
    private readonly EquilibraFitPlusPlusDbContext _dbContext;

    /// <summary>
    /// Initializes the repository.
    /// </summary>
    public AiCoachRepository(EquilibraFitPlusPlusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public void AdicionarSessao(ChatSession session)
    {
        _dbContext.ChatSessions.Add(session);
    }

    /// <inheritdoc />
    public Task<ChatSession?> ObterSessaoAsync(Guid tenantId, Guid usuarioId, Guid sessaoId, CancellationToken cancellationToken)
    {
        return QuerySessions()
            .FirstOrDefaultAsync(session => session.TenantId == tenantId && session.UsuarioId == usuarioId && session.Id == sessaoId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PagedResult<ChatSession>> ListarSessoesAsync(Guid tenantId, Guid usuarioId, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<ChatSession> query = QuerySessions()
            .Where(session => session.TenantId == tenantId && session.UsuarioId == usuarioId);

        int totalItems = await query.CountAsync(cancellationToken);
        ChatSession[] items = await query
            .OrderByDescending(session => session.CriadoEm)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<ChatSession>(items, page, pageSize, totalItems);
    }

    /// <inheritdoc />
    public async Task<AiCoachUserContext> ObterContextoAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken)
    {
        PerfilSaude? perfil = await _dbContext.PerfisSaude
            .AsNoTracking()
            .FirstOrDefaultAsync(perfil => perfil.TenantId == tenantId && perfil.UsuarioId == usuarioId, cancellationToken);

        PlanoUsuario? planoAtual = await _dbContext.PlanosUsuario
            .AsNoTracking()
            .OrderByDescending(plano => plano.Versao)
            .ThenByDescending(plano => plano.CriadoEm)
            .FirstOrDefaultAsync(plano => plano.TenantId == tenantId && plano.UsuarioId == usuarioId && plano.Status == StatusPlano.Ativo, cancellationToken);

        RegistroEvolucao? ultimaEvolucao = await _dbContext.RegistrosEvolucao
            .AsNoTracking()
            .OrderByDescending(evolucao => evolucao.Data)
            .FirstOrDefaultAsync(evolucao => evolucao.TenantId == tenantId && evolucao.UsuarioId == usuarioId, cancellationToken);

        DateTimeOffset inicioHistorico = DateTimeOffset.UtcNow.AddDays(-7);
        RegistroAlimentar[] registrosRecentes = await _dbContext.RegistrosAlimentares
            .AsNoTracking()
            .Where(registro => registro.TenantId == tenantId && registro.UsuarioId == usuarioId && registro.DataHora >= inicioHistorico)
            .OrderByDescending(registro => registro.DataHora)
            .Take(25)
            .ToArrayAsync(cancellationToken);

        return new AiCoachUserContext(perfil, planoAtual, ultimaEvolucao, registrosRecentes);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<AiCoachMemory>> ListarMemoriasAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken)
    {
        return await _dbContext.AiCoachMemories
            .AsNoTracking()
            .Where(memory =>
                memory.TenantId == tenantId
                && memory.UsuarioId == usuarioId
                && memory.TipoFato != AiCoachMemoryFactTypes.ModelInference
                && memory.TipoFato != AiCoachMemoryFactTypes.TemporaryContext
                && (memory.TipoFato == AiCoachMemoryFactTypes.ExplicitFact
                    || memory.TipoFato == AiCoachMemoryFactTypes.UserPreference
                    || memory.ConfirmadoPeloUsuario))
            .OrderBy(memory => memory.Categoria)
            .ThenBy(memory => memory.Chave)
            .Take(50)
            .ToArrayAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<AiCoachMemory?> ObterMemoriaAsync(Guid tenantId, Guid usuarioId, string chave, CancellationToken cancellationToken)
    {
        return _dbContext.AiCoachMemories
            .FirstOrDefaultAsync(memory =>
                memory.TenantId == tenantId
                && memory.UsuarioId == usuarioId
                && memory.Chave == chave,
                cancellationToken);
    }

    /// <inheritdoc />
    public void AdicionarMemoria(AiCoachMemory memoria)
    {
        _dbContext.AiCoachMemories.Add(memoria);
    }

    /// <inheritdoc />
    public void AdicionarExecutionLog(AiExecutionLog log)
    {
        _dbContext.AiExecutionLogs.Add(log);
    }

    private IQueryable<ChatSession> QuerySessions()
    {
        return _dbContext.ChatSessions
            .Include(session => session.Mensagens);
    }
}
