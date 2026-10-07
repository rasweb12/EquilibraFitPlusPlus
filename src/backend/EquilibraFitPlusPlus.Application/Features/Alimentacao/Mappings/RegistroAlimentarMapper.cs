using EquilibraFitPlusPlus.Contracts.Alimentacao;
using EquilibraFitPlusPlus.Domain.Entities;

namespace EquilibraFitPlusPlus.Application.Features.Alimentacao.Mappings;

/// <summary>
/// Maps food log entities to API contracts.
/// </summary>
internal static class RegistroAlimentarMapper
{
    /// <summary>
    /// Maps a food log to a response.
    /// </summary>
    public static RegistroAlimentarResponse Map(RegistroAlimentar registro)
    {
        return new RegistroAlimentarResponse(
            registro.Id,
            registro.DataHora,
            registro.TipoRefeicao.ToString(),
            registro.Origem.ToString(),
            registro.CaloriasTotal,
            registro.ProteinaTotalG,
            registro.CarboidratoTotalG,
            registro.GorduraTotalG,
            registro.ConfirmadoPeloUsuario,
            registro.Itens
                .OrderBy(item => item.Nome)
                .Select(item => new ItemAlimentarResponse(
                    item.Id,
                    item.Nome,
                    item.Quantidade,
                    item.Unidade,
                    item.Calorias,
                    item.ProteinaG,
                    item.CarboidratoG,
                    item.GorduraG,
                    item.FonteNutricional))
                .ToArray(),
            "Registro alimentar salvo. Sem pressa e sem culpa: usamos esses dados para ajustar melhor sua rotina.");
    }
}
