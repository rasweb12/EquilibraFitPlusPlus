using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Pagination;

namespace EquilibraFitPlusPlus.Application.Abstractions.Notificacoes;

/// <summary>
/// Provides notification persistence operations.
/// </summary>
public interface INotificacaoRepository
{
    /// <summary>Lists notifications for the current user.</summary>
    Task<PagedResult<Notificacao>> ListarUsuarioAsync(Guid tenantId, Guid usuarioId, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Lists notifications for administrative views.</summary>
    Task<PagedResult<Notificacao>> ListarAdminAsync(Guid tenantId, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Gets a user notification.</summary>
    Task<Notificacao?> ObterUsuarioNotificacaoAsync(Guid tenantId, Guid usuarioId, Guid notificacaoId, CancellationToken cancellationToken);

    /// <summary>Adds a notification.</summary>
    void Adicionar(Notificacao notificacao);
}
