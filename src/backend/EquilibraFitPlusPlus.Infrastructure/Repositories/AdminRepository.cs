using EquilibraFitPlusPlus.Application.Abstractions.Admin;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Domain.Enums;
using EquilibraFitPlusPlus.Infrastructure.Data;
using EquilibraFitPlusPlus.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace EquilibraFitPlusPlus.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation for administrative operations.
/// </summary>
public sealed class AdminRepository : IAdminRepository
{
    private readonly EquilibraFitPlusPlusDbContext _dbContext;

    /// <summary>
    /// Initializes the repository.
    /// </summary>
    public AdminRepository(EquilibraFitPlusPlusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<AdminDashboardSourceData> ObterDashboardAsync(Guid tenantId, DateOnly dataReferencia, CancellationToken cancellationToken)
    {
        DateTimeOffset dayStart = ToUtcStart(dataReferencia);
        DateTimeOffset dayEndExclusive = ToUtcStart(dataReferencia.AddDays(1));
        var monthStartDate = new DateOnly(dataReferencia.Year, dataReferencia.Month, 1);
        DateTimeOffset monthStart = ToUtcStart(monthStartDate);
        DateTimeOffset monthEndExclusive = ToUtcStart(monthStartDate.AddMonths(1));

        int usuariosTotal = await _dbContext.Usuarios.CountAsync(usuario => usuario.TenantId == tenantId, cancellationToken);
        int usuariosAtivos = await _dbContext.Usuarios.CountAsync(usuario => usuario.TenantId == tenantId && usuario.Status == UsuarioStatus.Ativo, cancellationToken);
        int perfisPreenchidos = await _dbContext.PerfisSaude.CountAsync(perfil => perfil.TenantId == tenantId, cancellationToken);
        int refeicoesHoje = await _dbContext.RegistrosAlimentares.CountAsync(registro =>
            registro.TenantId == tenantId && registro.DataHora >= dayStart && registro.DataHora < dayEndExclusive, cancellationToken);
        int treinosAtivos = await _dbContext.TreinosUsuario.CountAsync(treino => treino.TenantId == tenantId && treino.Ativo, cancellationToken);
        int sessoesCoachHoje = await _dbContext.ChatSessions.CountAsync(session =>
            session.TenantId == tenantId && session.CriadoEm >= dayStart && session.CriadoEm < dayEndExclusive, cancellationToken);
        int mensagensCoachHoje = await _dbContext.ChatMessages.CountAsync(message =>
            message.TenantId == tenantId && message.CriadoEm >= dayStart && message.CriadoEm < dayEndExclusive, cancellationToken);
        int assinaturasAtivas = await _dbContext.Assinaturas.CountAsync(assinatura =>
            assinatura.TenantId == tenantId && assinatura.Status == StatusAssinatura.Ativa, cancellationToken);
        decimal receitaMes = await _dbContext.Pagamentos
            .Where(pagamento => pagamento.TenantId == tenantId
                && pagamento.Status == StatusPagamento.Pago
                && pagamento.CriadoEm >= monthStart
                && pagamento.CriadoEm < monthEndExclusive)
            .SumAsync(pagamento => (decimal?)pagamento.Valor, cancellationToken) ?? 0m;

        return new AdminDashboardSourceData(
            usuariosTotal,
            usuariosAtivos,
            perfisPreenchidos,
            refeicoesHoje,
            treinosAtivos,
            sessoesCoachHoje,
            mensagensCoachHoje,
            assinaturasAtivas,
            receitaMes);
    }

    /// <inheritdoc />
    public async Task<PagedResult<Usuario>> ListarUsuariosAsync(Guid tenantId, string? termo, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<Usuario> query = _dbContext.Usuarios
            .Include(usuario => usuario.PerfilSaude)
            .Where(usuario => usuario.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(termo))
        {
            string normalizedTerm = termo.Trim();
            query = query.Where(usuario => usuario.Nome.Contains(normalizedTerm) || usuario.Email.Contains(normalizedTerm));
        }

        int totalItems = await query.CountAsync(cancellationToken);
        Usuario[] items = await query
            .OrderBy(usuario => usuario.Nome)
            .ThenBy(usuario => usuario.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<Usuario>(items, page, pageSize, totalItems);
    }

    /// <inheritdoc />
    public Task<Usuario?> ObterUsuarioAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken)
    {
        return _dbContext.Usuarios
            .FirstOrDefaultAsync(usuario => usuario.TenantId == tenantId && usuario.Id == usuarioId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> RevogarSessoesUsuarioAsync(Guid tenantId, Guid usuarioId, string? ip, CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var user = await ObterUsuarioAsync(tenantId, usuarioId, cancellationToken);
        if (user != null) user.SessoesRevogadasAntesDe = now;
        RefreshToken[] tokens = await _dbContext.RefreshTokens
            .Where(token => token.TenantId == tenantId
                && token.UsuarioId == usuarioId
                && token.RevogadoEm == null
                && token.ExpiraEm > now)
            .ToArrayAsync(cancellationToken);

        foreach (RefreshToken token in tokens)
        {
            token.RevogadoEm = now;
            token.RevogadoPorIp = ip;
        }

        var sessions = await _dbContext.AuthSessions
            .Where(x => x.TenantId == tenantId && x.UsuarioId == usuarioId && x.RevogadoEm == null)
            .ToArrayAsync(cancellationToken);
        foreach (var session in sessions) session.RevogadoEm = now;
        return tokens.Length + sessions.Length;
    }

    /// <inheritdoc />
    public async Task<PagedResult<Auditoria>> ListarAuditoriasAsync(Guid tenantId, string? entidade, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<Auditoria> query = _dbContext.Auditorias
            .Where(auditoria => auditoria.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(entidade))
        {
            string normalizedEntity = entidade.Trim();
            query = query.Where(auditoria => auditoria.Entidade.Contains(normalizedEntity));
        }

        int totalItems = await query.CountAsync(cancellationToken);
        Auditoria[] items = await query
            .OrderByDescending(auditoria => auditoria.CriadoEm)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<Auditoria>(items, page, pageSize, totalItems);
    }

    /// <inheritdoc />
    public async Task<PagedResult<FeatureFlag>> ListarFeatureFlagsAsync(Guid tenantId, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<FeatureFlag> query = _dbContext.FeatureFlags
            .Where(featureFlag => featureFlag.TenantId == tenantId);

        int totalItems = await query.CountAsync(cancellationToken);
        FeatureFlag[] items = await query
            .OrderBy(featureFlag => featureFlag.Chave)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<FeatureFlag>(items, page, pageSize, totalItems);
    }

    /// <inheritdoc />
    public Task<FeatureFlag?> ObterFeatureFlagAsync(Guid tenantId, string chave, CancellationToken cancellationToken)
    {
        return _dbContext.FeatureFlags
            .FirstOrDefaultAsync(featureFlag => featureFlag.TenantId == tenantId && featureFlag.Chave == chave, cancellationToken);
    }

    /// <inheritdoc />
    public void AdicionarFeatureFlag(FeatureFlag featureFlag)
    {
        _dbContext.FeatureFlags.Add(featureFlag);
    }

    /// <inheritdoc />
    public void AdicionarAuditoria(Auditoria auditoria)
    {
        _dbContext.Auditorias.Add(auditoria);
    }

    private static DateTimeOffset ToUtcStart(DateOnly date)
    {
        return new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    }
}
