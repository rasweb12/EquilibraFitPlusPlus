using EquilibraFitPlusPlus.Contracts.Habitos;
using EquilibraFitPlusPlus.Domain.Entities;

namespace EquilibraFitPlusPlus.Application.Features.Habitos.Mappings;

/// <summary>
/// Maps daily habits entities to contracts.
/// </summary>
public static class HabitosMapper
{
    /// <summary>
    /// Maps an existing daily habits log to a response.
    /// </summary>
    public static HabitosDiariosResponse Map(RegistroHabitos registro, string mensagem)
    {
        return new HabitosDiariosResponse(
            registro.Id,
            registro.Data,
            registro.AguaMl,
            registro.MetaAguaMl,
            registro.SonoHoras,
            registro.MetaSonoHoras,
            registro.Humor,
            registro.MeditacaoRealizada,
            registro.AlongamentoRealizado,
            mensagem);
    }

    /// <summary>
    /// Creates an empty response for dates without a saved log yet.
    /// </summary>
    public static HabitosDiariosResponse Empty(DateOnly data)
    {
        return new HabitosDiariosResponse(
            null,
            data,
            0,
            2700,
            0m,
            8m,
            3,
            false,
            false,
            "Hábitos prontos para registro. Vamos no seu ritmo.");
    }
}
