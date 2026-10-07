using EquilibraFitPlusPlus.Application.Abstractions.Relatorios;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Domain.Enums;
using EquilibraFitPlusPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EquilibraFitPlusPlus.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation for reports.
/// </summary>
public sealed class RelatoriosRepository : IRelatoriosRepository
{
    private readonly EquilibraFitPlusPlusDbContext _dbContext;

    /// <summary>Initializes the repository.</summary>
    public RelatoriosRepository(EquilibraFitPlusPlusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<RelatorioUsuarioSourceData> ObterRelatorioUsuarioAsync(Guid tenantId, Guid usuarioId, DateOnly inicio, DateOnly fim, CancellationToken cancellationToken)
    {
        DateTimeOffset start = ToUtcStart(inicio);
        DateTimeOffset endExclusive = ToUtcStart(fim.AddDays(1));

        RegistroAlimentar[] registros = await _dbContext.RegistrosAlimentares
            .Where(registro => registro.TenantId == tenantId && registro.UsuarioId == usuarioId && registro.DataHora >= start && registro.DataHora < endExclusive)
            .ToArrayAsync(cancellationToken);

        TreinoUsuario[] treinos = await _dbContext.TreinosUsuario
            .Where(treino => treino.TenantId == tenantId && treino.UsuarioId == usuarioId && treino.Ativo)
            .ToArrayAsync(cancellationToken);

        RegistroEvolucao[] evolucoes = await _dbContext.RegistrosEvolucao
            .Where(evolucao => evolucao.TenantId == tenantId && evolucao.UsuarioId == usuarioId && evolucao.Data >= inicio && evolucao.Data <= fim)
            .OrderBy(evolucao => evolucao.Data)
            .ToArrayAsync(cancellationToken);

        return new RelatorioUsuarioSourceData(registros, treinos, evolucoes);
    }

    /// <inheritdoc />
    public async Task<RelatorioAdminSourceData> ObterRelatorioAdminAsync(Guid tenantId, DateOnly inicio, DateOnly fim, CancellationToken cancellationToken)
    {
        DateTimeOffset start = ToUtcStart(inicio);
        DateTimeOffset endExclusive = ToUtcStart(fim.AddDays(1));

        int usuariosAtivos = await _dbContext.Usuarios.CountAsync(usuario => usuario.TenantId == tenantId && usuario.Status == UsuarioStatus.Ativo, cancellationToken);
        int novosUsuarios = await _dbContext.Usuarios.CountAsync(usuario => usuario.TenantId == tenantId && usuario.CriadoEm >= start && usuario.CriadoEm < endExclusive, cancellationToken);
        int refeicoes = await _dbContext.RegistrosAlimentares.CountAsync(registro => registro.TenantId == tenantId && registro.DataHora >= start && registro.DataHora < endExclusive, cancellationToken);
        int planos = await _dbContext.PlanosUsuario.CountAsync(plano => plano.TenantId == tenantId && plano.CriadoEm >= start && plano.CriadoEm < endExclusive, cancellationToken);
        int treinos = await _dbContext.TreinosUsuario.CountAsync(treino => treino.TenantId == tenantId && treino.Ativo, cancellationToken);
        int sessoesCoach = await _dbContext.ChatSessions.CountAsync(session => session.TenantId == tenantId && session.CriadoEm >= start && session.CriadoEm < endExclusive, cancellationToken);
        int imagens = await _dbContext.AnalisesRefeicaoImagem.CountAsync(analise => analise.TenantId == tenantId && analise.CriadoEm >= start && analise.CriadoEm < endExclusive, cancellationToken);
        decimal receita = await _dbContext.Pagamentos
            .Where(pagamento => pagamento.TenantId == tenantId && pagamento.Status == StatusPagamento.Pago && pagamento.CriadoEm >= start && pagamento.CriadoEm < endExclusive)
            .SumAsync(pagamento => (decimal?)pagamento.Valor, cancellationToken) ?? 0m;

        return new RelatorioAdminSourceData(usuariosAtivos, novosUsuarios, refeicoes, planos, treinos, sessoesCoach, imagens, receita);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<UsuarioEngajadoSourceData>> ListarUsuariosEngajadosAsync(Guid tenantId, DateOnly inicio, DateOnly fim, int take, CancellationToken cancellationToken)
    {
        DateTimeOffset start = ToUtcStart(inicio);
        DateTimeOffset endExclusive = ToUtcStart(fim.AddDays(1));

        var registros = await _dbContext.RegistrosAlimentares
            .Where(registro => registro.TenantId == tenantId && registro.DataHora >= start && registro.DataHora < endExclusive)
            .GroupBy(registro => registro.UsuarioId)
            .Select(group => new { UsuarioId = group.Key, Total = group.Count() })
            .ToArrayAsync(cancellationToken);

        Guid[] userIds = registros.Select(item => item.UsuarioId).ToArray();
        Dictionary<Guid, Usuario> users = await _dbContext.Usuarios
            .Where(usuario => usuario.TenantId == tenantId && userIds.Contains(usuario.Id))
            .ToDictionaryAsync(usuario => usuario.Id, cancellationToken);

        Dictionary<Guid, int> activeWorkouts = await _dbContext.TreinosUsuario
            .Where(treino => treino.TenantId == tenantId && userIds.Contains(treino.UsuarioId) && treino.Ativo)
            .GroupBy(treino => treino.UsuarioId)
            .Select(group => new { UsuarioId = group.Key, Total = group.Count() })
            .ToDictionaryAsync(item => item.UsuarioId, item => item.Total, cancellationToken);

        return registros
            .Where(item => users.ContainsKey(item.UsuarioId))
            .Select(item => new UsuarioEngajadoSourceData(
                item.UsuarioId,
                users[item.UsuarioId].Nome,
                item.Total,
                activeWorkouts.GetValueOrDefault(item.UsuarioId)))
            .OrderByDescending(item => item.RegistrosAlimentares + item.TreinosAtivos)
            .Take(take)
            .ToArray();
    }

    private static DateTimeOffset ToUtcStart(DateOnly date)
    {
        return new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    }
}
