using EquilibraFitPlusPlus.Application.Abstractions.Lgpd;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Domain.Enums;
using EquilibraFitPlusPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EquilibraFitPlusPlus.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation for LGPD operations.
/// </summary>
public sealed class LgpdRepository : ILgpdRepository
{
    private readonly EquilibraFitPlusPlusDbContext _dbContext;

    /// <summary>Initializes the repository.</summary>
    public LgpdRepository(EquilibraFitPlusPlusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<LgpdExportSourceData?> ObterExportacaoAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken)
    {
        Usuario? usuario = await _dbContext.Usuarios
            .Include(item => item.PerfilSaude)
            .FirstOrDefaultAsync(item => item.TenantId == tenantId && item.Id == usuarioId, cancellationToken);
        if (usuario is null)
        {
            return null;
        }

        ConsentimentoUsuario[] consentimentos = await _dbContext.ConsentimentosUsuario
            .Where(item => item.TenantId == tenantId && item.UsuarioId == usuarioId)
            .OrderBy(item => item.Tipo)
            .ThenByDescending(item => item.AceitoEm)
            .ToArrayAsync(cancellationToken);

        int registros = await _dbContext.RegistrosAlimentares.CountAsync(item => item.TenantId == tenantId && item.UsuarioId == usuarioId, cancellationToken);
        int treinos = await _dbContext.TreinosUsuario.CountAsync(item => item.TenantId == tenantId && item.UsuarioId == usuarioId, cancellationToken);
        int planos = await _dbContext.PlanosUsuario.CountAsync(item => item.TenantId == tenantId && item.UsuarioId == usuarioId, cancellationToken);
        int sessoes = await _dbContext.ChatSessions.CountAsync(item => item.TenantId == tenantId && item.UsuarioId == usuarioId, cancellationToken);
        int notificacoes = await _dbContext.Notificacoes.CountAsync(item => item.TenantId == tenantId && item.UsuarioId == usuarioId, cancellationToken);

        return new LgpdExportSourceData(usuario, usuario.PerfilSaude, consentimentos, registros, treinos, planos, sessoes, notificacoes);
    }

    /// <inheritdoc />
    public async Task<LgpdAdminSourceData> ObterResumoAdminAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        int usuariosAtivos = await _dbContext.Usuarios.CountAsync(item => item.TenantId == tenantId && item.Status == UsuarioStatus.Ativo, cancellationToken);
        int consentimentos = await _dbContext.ConsentimentosUsuario.CountAsync(item => item.TenantId == tenantId, cancellationToken);
        DateTimeOffset last30Days = DateTimeOffset.UtcNow.AddDays(-30);
        int exportacoes = await _dbContext.Auditorias.CountAsync(item =>
            item.TenantId == tenantId
            && item.Acao == "lgpd.exportar"
            && item.CriadoEm >= last30Days,
            cancellationToken);
        int exclusoes = await _dbContext.Auditorias.CountAsync(item =>
            item.TenantId == tenantId
            && item.Acao == "lgpd.exclusao.solicitada",
            cancellationToken);

        return new LgpdAdminSourceData(usuariosAtivos, consentimentos, exportacoes, exclusoes);
    }
}
