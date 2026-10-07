using EquilibraFitPlusPlus.Application.Abstractions.Admin;
using EquilibraFitPlusPlus.Contracts.Admin;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Mappings;

/// <summary>
/// Maps AI operation source data to administrative contracts.
/// </summary>
internal static class AdminIaMapper
{
    /// <summary>
    /// Maps the full AI operation overview.
    /// </summary>
    public static AdminIaOperacaoResponse Map(
        AdminIaServicoStatusSourceData status,
        AdminIaMetricasSourceData metrics,
        IReadOnlyCollection<AdminIaHistoricoSourceData> history,
        AdminIaEnsinoSourceData teaching)
    {
        return new AdminIaOperacaoResponse(
            Map(status),
            Map(metrics),
            history.Select(Map).ToArray(),
            Map(teaching));
    }

    /// <summary>
    /// Maps AI teaching configuration.
    /// </summary>
    public static AdminIaEnsinoResponse Map(AdminIaEnsinoSourceData source)
    {
        return new AdminIaEnsinoResponse(
            source.Versao,
            source.InstrucoesCoach,
            source.RegrasAlimentacao,
            source.RegrasTreino,
            source.BaseConhecimento,
            source.ExemplosBoasRespostas,
            source.Publicada,
            source.PublicadaEm);
    }

    /// <summary>
    /// Maps AI teaching version.
    /// </summary>
    public static AdminIaEnsinoVersaoResponse Map(AdminIaEnsinoVersaoSourceData source)
    {
        return new AdminIaEnsinoVersaoResponse(source.Versao, source.PublicadaEm, source.Observacao);
    }

    private static AdminIaServicoStatusResponse Map(AdminIaServicoStatusSourceData source)
    {
        return new AdminIaServicoStatusResponse(
            source.Configurada,
            source.Online,
            source.LatenciaMs,
            source.BaseUrl,
            source.Modelo,
            source.Mensagem);
    }

    private static AdminIaMetricasResponse Map(AdminIaMetricasSourceData source)
    {
        return new AdminIaMetricasResponse(
            source.SessoesCoachHoje,
            source.MensagensCoachHoje,
            source.ReconhecimentosImagemHoje,
            source.PlanosGeradosHoje,
            source.TreinosGeradosHoje,
            source.FallbacksHoje,
            source.RespostasBloqueadas30Dias);
    }

    private static AdminIaHistoricoSanitizadoResponse Map(AdminIaHistoricoSourceData source)
    {
        return new AdminIaHistoricoSanitizadoResponse(
            source.Id,
            source.Tipo,
            source.Resumo,
            source.Status,
            source.Modelo,
            source.CriadoEm);
    }
}
