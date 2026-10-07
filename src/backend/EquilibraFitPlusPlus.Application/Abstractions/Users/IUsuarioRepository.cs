using EquilibraFitPlusPlus.Domain.Entities;

namespace EquilibraFitPlusPlus.Application.Abstractions.Users;

/// <summary>
/// Provides user read access needed by use cases.
/// </summary>
public interface IUsuarioRepository
{
    /// <summary>
    /// Gets an active business user by tenant and user identifier.
    /// </summary>
    Task<Usuario?> ObterAtivoAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken);
}
