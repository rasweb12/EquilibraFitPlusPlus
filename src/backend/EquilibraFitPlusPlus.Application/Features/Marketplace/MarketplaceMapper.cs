using EquilibraFitPlusPlus.Contracts.Marketplace;
using EquilibraFitPlusPlus.Domain.Entities;

namespace EquilibraFitPlusPlus.Application.Features.Marketplace;

/// <summary>
/// Maps marketplace entities.
/// </summary>
internal static class MarketplaceMapper
{
    /// <summary>Maps a partner.</summary>
    public static ParceiroResponse Map(Parceiro parceiro)
    {
        return new ParceiroResponse(
            parceiro.Id,
            parceiro.Nome,
            parceiro.Tipo.ToString(),
            parceiro.Status.ToString(),
            parceiro.EmailContato,
            parceiro.Documento);
    }
}
