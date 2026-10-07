using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Pagination;

namespace EquilibraFitPlusPlus.Application.Abstractions.Admin;

/// <summary>
/// Provides administrative read and write operations.
/// </summary>
public interface IAdminRepository
{
    /// <summary>
    /// Gets administrative dashboard source data.
    /// </summary>
    Task<AdminDashboardSourceData> ObterDashboardAsync(Guid tenantId, DateOnly dataReferencia, CancellationToken cancellationToken);

    /// <summary>
    /// Lists users for administrative search.
    /// </summary>
    Task<PagedResult<Usuario>> ListarUsuariosAsync(Guid tenantId, string? termo, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a user for administrative operations.
    /// </summary>
    Task<Usuario?> ObterUsuarioAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes active refresh tokens for a user.
    /// </summary>
    Task<int> RevogarSessoesUsuarioAsync(Guid tenantId, Guid usuarioId, string? ip, CancellationToken cancellationToken);

    /// <summary>
    /// Lists audit logs.
    /// </summary>
    Task<PagedResult<Auditoria>> ListarAuditoriasAsync(Guid tenantId, string? entidade, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Lists feature flags.
    /// </summary>
    Task<PagedResult<FeatureFlag>> ListarFeatureFlagsAsync(Guid tenantId, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a feature flag by key.
    /// </summary>
    Task<FeatureFlag?> ObterFeatureFlagAsync(Guid tenantId, string chave, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a feature flag.
    /// </summary>
    void AdicionarFeatureFlag(FeatureFlag featureFlag);

    /// <summary>
    /// Adds an audit log.
    /// </summary>
    void AdicionarAuditoria(Auditoria auditoria);
}

/// <summary>
/// Administrative dashboard source data.
/// </summary>
public sealed record AdminDashboardSourceData(
    int UsuariosTotal,
    int UsuariosAtivos,
    int PerfisPreenchidos,
    int RefeicoesHoje,
    int TreinosAtivos,
    int SessoesCoachHoje,
    int MensagensCoachHoje,
    int AssinaturasAtivas,
    decimal ReceitaMes);
