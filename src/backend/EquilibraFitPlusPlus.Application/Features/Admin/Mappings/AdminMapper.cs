using EquilibraFitPlusPlus.Application.Abstractions.Admin;
using EquilibraFitPlusPlus.Contracts.Admin;
using EquilibraFitPlusPlus.Domain.Entities;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Mappings;

/// <summary>
/// Maps administrative domain data to API contracts.
/// </summary>
internal static class AdminMapper
{
    /// <summary>
    /// Maps dashboard source data.
    /// </summary>
    public static AdminDashboardResponse Map(AdminDashboardSourceData source)
    {
        string[] alertas = BuildAlerts(source);
        return new AdminDashboardResponse(
            source.UsuariosTotal,
            source.UsuariosAtivos,
            source.PerfisPreenchidos,
            source.RefeicoesHoje,
            source.TreinosAtivos,
            source.SessoesCoachHoje,
            source.MensagensCoachHoje,
            source.AssinaturasAtivas,
            source.ReceitaMes,
            alertas);
    }

    /// <summary>
    /// Maps a user summary.
    /// </summary>
    public static AdminUsuarioResumoResponse Map(Usuario usuario)
    {
        return new AdminUsuarioResumoResponse(
            usuario.Id,
            usuario.Nome,
            usuario.Email,
            usuario.Status.ToString(),
            usuario.Role.ToString(),
            usuario.CriadoEm,
            usuario.UltimoLoginEm,
            usuario.PerfilSaude is not null);
    }

    /// <summary>
    /// Maps a feature flag.
    /// </summary>
    public static FeatureFlagResponse Map(FeatureFlag featureFlag)
    {
        return new FeatureFlagResponse(
            featureFlag.Id,
            featureFlag.Chave,
            featureFlag.Habilitada,
            featureFlag.ConfiguracaoJson,
            featureFlag.CriadoEm,
            featureFlag.AtualizadoEm);
    }

    /// <summary>
    /// Maps an audit log entry.
    /// </summary>
    public static AuditoriaResponse Map(Auditoria auditoria)
    {
        return new AuditoriaResponse(
            auditoria.Id,
            auditoria.UsuarioId,
            auditoria.Acao,
            auditoria.Entidade,
            auditoria.EntidadeId,
            auditoria.MetadadosJson,
            auditoria.Ip,
            auditoria.UserAgent,
            auditoria.CriadoEm);
    }

    private static string[] BuildAlerts(AdminDashboardSourceData source)
    {
        var alertas = new List<string>();

        if (source.UsuariosTotal > 0 && source.PerfisPreenchidos < source.UsuariosTotal / 2)
        {
            alertas.Add("Há usuários sem questionário completo. Vale acompanhar o funil de onboarding.");
        }

        if (source.MensagensCoachHoje > 1000)
        {
            alertas.Add("Uso do Coach IA elevado hoje. Acompanhe custos, latencia e limites operacionais.");
        }

        if (source.AssinaturasAtivas == 0 && source.UsuariosAtivos > 100)
        {
            alertas.Add("Base ativa crescendo sem assinaturas. Planeje a habilitacao gradual da monetizacao.");
        }

        if (alertas.Count == 0)
        {
            alertas.Add("Operação sem alertas críticos no período consultado.");
        }

        return alertas.ToArray();
    }
}
