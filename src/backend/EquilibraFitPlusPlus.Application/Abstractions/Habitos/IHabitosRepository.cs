using EquilibraFitPlusPlus.Domain.Entities;

namespace EquilibraFitPlusPlus.Application.Abstractions.Habitos;

/// <summary>
/// Provides persistence operations for user daily habits.
/// </summary>
public interface IHabitosRepository
{
    /// <summary>
    /// Gets a daily habits log by user and date.
    /// </summary>
    Task<RegistroHabitos?> ObterPorDataAsync(Guid tenantId, Guid usuarioId, DateOnly data, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a daily habits log.
    /// </summary>
    void Adicionar(RegistroHabitos registro);
}
