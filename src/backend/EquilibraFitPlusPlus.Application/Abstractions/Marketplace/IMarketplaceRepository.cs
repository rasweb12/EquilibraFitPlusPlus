using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Pagination;

namespace EquilibraFitPlusPlus.Application.Abstractions.Marketplace;

/// <summary>
/// Provides marketplace partner persistence operations.
/// </summary>
public interface IMarketplaceRepository
{
    /// <summary>Lists partners.</summary>
    Task<PagedResult<Parceiro>> ListarParceirosAsync(Guid tenantId, string? termo, bool somenteAprovados, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Gets a partner by identifier.</summary>
    Task<Parceiro?> ObterParceiroAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    /// <summary>Adds a partner.</summary>
    void AdicionarParceiro(Parceiro parceiro);
}
