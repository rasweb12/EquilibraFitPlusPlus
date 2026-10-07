using EquilibraFitPlusPlus.Application.Abstractions.Marketplace;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Infrastructure.Data;
using EquilibraFitPlusPlus.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace EquilibraFitPlusPlus.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation for marketplace operations.
/// </summary>
public sealed class MarketplaceRepository : IMarketplaceRepository
{
    private readonly EquilibraFitPlusPlusDbContext _dbContext;

    /// <summary>Initializes the repository.</summary>
    public MarketplaceRepository(EquilibraFitPlusPlusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<PagedResult<Parceiro>> ListarParceirosAsync(Guid tenantId, string? termo, bool somenteAprovados, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<Parceiro> query = _dbContext.Parceiros.Where(parceiro => parceiro.TenantId == tenantId);
        if (somenteAprovados)
        {
            query = query.Where(parceiro => parceiro.Status == Domain.Enums.StatusParceiro.Aprovado);
        }

        if (!string.IsNullOrWhiteSpace(termo))
        {
            string normalizedTerm = termo.Trim();
            query = query.Where(parceiro => parceiro.Nome.Contains(normalizedTerm) || (parceiro.EmailContato != null && parceiro.EmailContato.Contains(normalizedTerm)));
        }

        int totalItems = await query.CountAsync(cancellationToken);
        Parceiro[] items = await query
            .OrderBy(parceiro => parceiro.Nome)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<Parceiro>(items, page, pageSize, totalItems);
    }

    /// <inheritdoc />
    public Task<Parceiro?> ObterParceiroAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Parceiros.FirstOrDefaultAsync(parceiro => parceiro.TenantId == tenantId && parceiro.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public void AdicionarParceiro(Parceiro parceiro)
    {
        _dbContext.Parceiros.Add(parceiro);
    }
}
