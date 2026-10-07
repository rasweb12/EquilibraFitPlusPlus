using EquilibraFitPlusPlus.Domain.Entities;

namespace EquilibraFitPlusPlus.Application.Abstractions.Onboarding;

/// <summary>
/// Provides persistence operations for the user onboarding questionnaire.
/// </summary>
public interface IOnboardingRepository
{
    /// <summary>
    /// Gets the health profile for a user.
    /// </summary>
    Task<PerfilSaude?> ObterPorUsuarioAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new health profile.
    /// </summary>
    void Adicionar(PerfilSaude perfilSaude);
}
