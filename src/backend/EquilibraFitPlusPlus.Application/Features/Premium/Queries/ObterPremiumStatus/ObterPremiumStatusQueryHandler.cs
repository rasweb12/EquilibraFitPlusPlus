using EquilibraFitPlusPlus.Application.Abstractions.Premium;
using EquilibraFitPlusPlus.Contracts.Premium;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Premium.Queries.ObterPremiumStatus;

/// <summary>
/// Handles premium status reads.
/// </summary>
public sealed class ObterPremiumStatusQueryHandler : IRequestHandler<ObterPremiumStatusQuery, Result<PremiumStatusResponse>>
{
    private readonly IPremiumRepository _premiumRepository;

    /// <summary>Initializes the handler.</summary>
    public ObterPremiumStatusQueryHandler(IPremiumRepository premiumRepository)
    {
        _premiumRepository = premiumRepository;
    }

    /// <inheritdoc />
    public async Task<Result<PremiumStatusResponse>> Handle(ObterPremiumStatusQuery request, CancellationToken cancellationToken)
    {
        Assinatura? assinatura = await _premiumRepository.ObterAssinaturaAtivaAsync(request.TenantId, request.UsuarioId, cancellationToken);
        bool premium = assinatura is not null && !assinatura.PlanoCodigo.Equals("FREE", StringComparison.OrdinalIgnoreCase);

        return Result<PremiumStatusResponse>.Success(new PremiumStatusResponse(
            assinatura?.Id,
            assinatura?.PlanoCodigo ?? "FREE",
            assinatura?.Status.ToString() ?? "Ativa",
            premium,
            assinatura?.TerminaEm,
            premium ? 50 : 3,
            premium ? 200 : 10,
            premium
                ? "Premium ativo. Vamos usar recursos avançados para adaptar sua rotina com mais profundidade."
                : "Plano gratuito ativo. Você continua com valor real e pode evoluir no seu ritmo."));
    }
}
