using EquilibraFitPlusPlus.Domain.Entities;

namespace EquilibraFitPlusPlus.Application.Abstractions.Planos;

/// <summary>
/// Provides persistence operations for generated diet plans.
/// </summary>
public interface IPlanoAlimentarRepository
{
    /// <summary>
    /// Gets the current health profile for plan generation.
    /// </summary>
    Task<PerfilSaude?> ObterPerfilAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the current active diet plan.
    /// </summary>
    Task<PlanoUsuario?> ObterPlanoAtivoAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the next version number for a user's plan.
    /// </summary>
    Task<int> ObterProximaVersaoAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a generated diet plan.
    /// </summary>
    void AdicionarPlano(PlanoUsuario plano);
}
