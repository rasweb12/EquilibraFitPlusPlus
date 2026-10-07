using EquilibraFitPlusPlus.Contracts.Planos;
using EquilibraFitPlusPlus.Domain.Entities;

namespace EquilibraFitPlusPlus.Application.Features.Planos.Mappings;

/// <summary>
/// Maps diet plan entities to API contracts.
/// </summary>
internal static class PlanoAlimentarMapper
{
    /// <summary>
    /// Maps a generated diet plan.
    /// </summary>
    public static PlanoAlimentarGeradoResponse Map(
        PlanoUsuario plano,
        MetaNutricional meta,
        IReadOnlyCollection<SugestaoRefeicaoResponse> sugestoes,
        IReadOnlyCollection<string> alternativas,
        IReadOnlyCollection<string> avisos)
    {
        return new PlanoAlimentarGeradoResponse(
            plano.Id,
            plano.Versao,
            plano.CaloriasDia,
            plano.ObjetivoSemanalKg,
            plano.FonteGeracao,
            plano.ModeloIaVersao,
            new MetaNutricionalResponse(
                meta.ProteinaG,
                meta.CarboidratoG,
                meta.GorduraG,
                meta.FibraG,
                meta.AguaMl),
            sugestoes,
            alternativas,
            avisos,
            "Plano alimentar gerado. Podemos ajustar por rotina, preferências e acompanhamento profissional quando necessário.");
    }
}
