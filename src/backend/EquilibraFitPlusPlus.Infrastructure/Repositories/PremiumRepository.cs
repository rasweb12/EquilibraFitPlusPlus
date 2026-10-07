using EquilibraFitPlusPlus.Application.Abstractions.Premium;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Domain.Enums;
using EquilibraFitPlusPlus.Infrastructure.Data;
using EquilibraFitPlusPlus.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace EquilibraFitPlusPlus.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation for premium operations.
/// </summary>
public sealed class PremiumRepository : IPremiumRepository
{
    private readonly EquilibraFitPlusPlusDbContext _dbContext;

    /// <summary>Initializes the repository.</summary>
    public PremiumRepository(EquilibraFitPlusPlusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public Task<Assinatura?> ObterAssinaturaAtivaAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return _dbContext.Assinaturas
            .OrderByDescending(assinatura => assinatura.InicioEm)
            .FirstOrDefaultAsync(
                assinatura => assinatura.TenantId == tenantId
                    && assinatura.UsuarioId == usuarioId
                    && assinatura.Status == StatusAssinatura.Ativa
                    && (assinatura.Plataforma != "GOOGLE_PLAY" || assinatura.ExpiracaoUtc > now)
                    && (assinatura.TerminaEm == null || assinatura.TerminaEm >= today),
                cancellationToken);
    }

    /// <inheritdoc />
    public Task<PagedResult<Assinatura>> ListarAssinaturasAsync(Guid tenantId, int page, int pageSize, CancellationToken cancellationToken)
    {
        return PageAsync(
            _dbContext.Assinaturas.Where(assinatura => assinatura.TenantId == tenantId).OrderByDescending(assinatura => assinatura.CriadoEm),
            page,
            pageSize,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<PagedResult<Pagamento>> ListarPagamentosAsync(Guid tenantId, int page, int pageSize, CancellationToken cancellationToken)
    {
        return PageAsync(
            _dbContext.Pagamentos.Where(pagamento => pagamento.TenantId == tenantId).OrderByDescending(pagamento => pagamento.CriadoEm),
            page,
            pageSize,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<PagedResult<Cupom>> ListarCuponsAsync(Guid tenantId, int page, int pageSize, CancellationToken cancellationToken)
    {
        return PageAsync(
            _dbContext.Cupons.Where(cupom => cupom.TenantId == tenantId).OrderBy(cupom => cupom.Codigo),
            page,
            pageSize,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Cupom?> ObterCupomPorCodigoAsync(Guid tenantId, string codigo, CancellationToken cancellationToken)
    {
        return _dbContext.Cupons
            .FirstOrDefaultAsync(cupom => cupom.TenantId == tenantId && cupom.Codigo == codigo, cancellationToken);
    }

    /// <inheritdoc />
    public void AdicionarCupom(Cupom cupom)
    {
        _dbContext.Cupons.Add(cupom);
    }

    private static async Task<PagedResult<T>> PageAsync<T>(IQueryable<T> query, int page, int pageSize, CancellationToken cancellationToken)
    {
        int totalItems = await query.CountAsync(cancellationToken);
        T[] items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(cancellationToken);
        return new PagedResult<T>(items, page, pageSize, totalItems);
    }
}
