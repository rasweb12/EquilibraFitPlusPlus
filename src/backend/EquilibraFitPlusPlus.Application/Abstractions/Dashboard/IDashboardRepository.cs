using EquilibraFitPlusPlus.Domain.Entities;

namespace EquilibraFitPlusPlus.Application.Abstractions.Dashboard;

/// <summary>
/// Provides optimized reads for the user dashboard.
/// </summary>
public interface IDashboardRepository
{
    /// <summary>
    /// Gets dashboard source data for a user and date.
    /// </summary>
    Task<DashboardSourceData> ObterResumoAsync(Guid tenantId, Guid usuarioId, DateOnly data, CancellationToken cancellationToken);
}

/// <summary>
/// Source data used to compose the user dashboard.
/// </summary>
public sealed record DashboardSourceData(
    PerfilSaude? Perfil,
    PlanoUsuario? PlanoAtual,
    IReadOnlyCollection<RegistroAlimentar> RegistrosAlimentares,
    IReadOnlyCollection<TreinoUsuario> TreinosAtivos,
    RegistroEvolucao? UltimaEvolucao,
    RegistroEvolucao? EvolucaoAnterior);
