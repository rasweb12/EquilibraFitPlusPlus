using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Pagination;

namespace EquilibraFitPlusPlus.Application.Abstractions.Premium;

/// <summary>
/// Provides premium, subscription, payment and coupon persistence operations.
/// </summary>
public interface IPremiumRepository
{
    /// <summary>Gets the active subscription for a user.</summary>
    Task<Assinatura?> ObterAssinaturaAtivaAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>Lists subscriptions for administrative views.</summary>
    Task<PagedResult<Assinatura>> ListarAssinaturasAsync(Guid tenantId, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Lists payments for administrative views.</summary>
    Task<PagedResult<Pagamento>> ListarPagamentosAsync(Guid tenantId, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Lists coupons for administrative views.</summary>
    Task<PagedResult<Cupom>> ListarCuponsAsync(Guid tenantId, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Gets a coupon by code.</summary>
    Task<Cupom?> ObterCupomPorCodigoAsync(Guid tenantId, string codigo, CancellationToken cancellationToken);

    /// <summary>Adds a coupon.</summary>
    void AdicionarCupom(Cupom cupom);
}
