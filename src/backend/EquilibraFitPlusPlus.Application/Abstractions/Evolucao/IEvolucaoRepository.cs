using EquilibraFitPlusPlus.Domain.Entities;

namespace EquilibraFitPlusPlus.Application.Abstractions.Evolucao;

/// <summary>
/// Provides persistence operations for user body evolution logs.
/// </summary>
public interface IEvolucaoRepository
{
    /// <summary>
    /// Gets a progress log by user and date.
    /// </summary>
    Task<RegistroEvolucao?> ObterPorDataAsync(Guid tenantId, Guid usuarioId, DateOnly data, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a progress log.
    /// </summary>
    void Adicionar(RegistroEvolucao registro);
}
