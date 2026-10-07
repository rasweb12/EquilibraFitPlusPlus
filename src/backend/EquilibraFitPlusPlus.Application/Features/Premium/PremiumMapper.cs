using EquilibraFitPlusPlus.Contracts.Premium;
using EquilibraFitPlusPlus.Domain.Entities;

namespace EquilibraFitPlusPlus.Application.Features.Premium;

/// <summary>
/// Maps premium entities to API contracts.
/// </summary>
internal static class PremiumMapper
{
    /// <summary>Maps a subscription.</summary>
    public static AssinaturaResumoResponse Map(Assinatura assinatura)
    {
        return new AssinaturaResumoResponse(
            assinatura.Id,
            assinatura.UsuarioId,
            assinatura.PlanoCodigo,
            assinatura.Status.ToString(),
            assinatura.InicioEm,
            assinatura.TerminaEm,
            assinatura.ProviderId);
    }

    /// <summary>Maps a payment.</summary>
    public static PagamentoResumoResponse Map(Pagamento pagamento)
    {
        return new PagamentoResumoResponse(
            pagamento.Id,
            pagamento.AssinaturaId,
            pagamento.Provider,
            pagamento.Metodo,
            pagamento.Valor,
            pagamento.Moeda,
            pagamento.Status.ToString(),
            pagamento.TransacaoExternaId);
    }

    /// <summary>Maps a coupon.</summary>
    public static CupomResponse Map(Cupom cupom)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        bool active = (cupom.ExpiraEm is null || cupom.ExpiraEm >= today)
            && (cupom.UsoMaximo is null || cupom.UsoAtual < cupom.UsoMaximo);

        return new CupomResponse(
            cupom.Id,
            cupom.Codigo,
            cupom.PercentualDesconto,
            cupom.ValorDesconto,
            cupom.ExpiraEm,
            cupom.UsoMaximo,
            cupom.UsoAtual,
            active);
    }
}
