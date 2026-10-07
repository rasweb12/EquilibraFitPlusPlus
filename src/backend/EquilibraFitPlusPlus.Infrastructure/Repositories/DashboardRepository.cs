using EquilibraFitPlusPlus.Application.Abstractions.Dashboard;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Domain.Enums;
using EquilibraFitPlusPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EquilibraFitPlusPlus.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation for dashboard read models.
/// </summary>
public sealed class DashboardRepository : IDashboardRepository
{
    private readonly EquilibraFitPlusPlusDbContext _dbContext;

    /// <summary>
    /// Initializes the repository.
    /// </summary>
    public DashboardRepository(EquilibraFitPlusPlusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<DashboardSourceData> ObterResumoAsync(Guid tenantId, Guid usuarioId, DateOnly data, CancellationToken cancellationToken)
    {
        DateTimeOffset start = ToUtcStart(data);
        DateTimeOffset endExclusive = ToUtcStart(data.AddDays(1));

        PerfilSaude? perfil = await _dbContext.PerfisSaude
            .FirstOrDefaultAsync(item => item.TenantId == tenantId && item.UsuarioId == usuarioId, cancellationToken);

        PlanoUsuario? plano = await _dbContext.PlanosUsuario
            .OrderByDescending(item => item.Versao)
            .ThenByDescending(item => item.CriadoEm)
            .FirstOrDefaultAsync(item => item.TenantId == tenantId && item.UsuarioId == usuarioId && item.Status == StatusPlano.Ativo, cancellationToken);

        RegistroAlimentar[] registros = await _dbContext.RegistrosAlimentares
            .Where(item => item.TenantId == tenantId && item.UsuarioId == usuarioId && item.DataHora >= start && item.DataHora < endExclusive)
            .ToArrayAsync(cancellationToken);

        TreinoUsuario[] treinosAtivos = await _dbContext.TreinosUsuario
            .Where(item => item.TenantId == tenantId && item.UsuarioId == usuarioId && item.Ativo)
            .ToArrayAsync(cancellationToken);

        RegistroEvolucao[] evolucoes = await _dbContext.RegistrosEvolucao
            .Where(item => item.TenantId == tenantId && item.UsuarioId == usuarioId)
            .OrderByDescending(item => item.Data)
            .Take(2)
            .ToArrayAsync(cancellationToken);

        return new DashboardSourceData(
            perfil,
            plano,
            registros,
            treinosAtivos,
            evolucoes.ElementAtOrDefault(0),
            evolucoes.ElementAtOrDefault(1));
    }

    private static DateTimeOffset ToUtcStart(DateOnly date)
    {
        return new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    }
}
