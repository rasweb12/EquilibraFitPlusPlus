using EquilibraFitPlusPlus.Domain.Entities;

namespace EquilibraFitPlusPlus.Application.Abstractions.Lgpd;

/// <summary>
/// Provides LGPD operational data access.
/// </summary>
public interface ILgpdRepository
{
    /// <summary>Gets export source data for a user.</summary>
    Task<LgpdExportSourceData?> ObterExportacaoAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>Gets administrative LGPD summary.</summary>
    Task<LgpdAdminSourceData> ObterResumoAdminAsync(Guid tenantId, CancellationToken cancellationToken);
}

/// <summary>
/// Source data for LGPD export.
/// </summary>
public sealed record LgpdExportSourceData(
    Usuario Usuario,
    PerfilSaude? PerfilSaude,
    IReadOnlyCollection<ConsentimentoUsuario> Consentimentos,
    int RegistrosAlimentares,
    int Treinos,
    int Planos,
    int SessoesCoach,
    int Notificacoes);

/// <summary>
/// Source data for administrative LGPD status.
/// </summary>
public sealed record LgpdAdminSourceData(
    int UsuariosAtivos,
    int ConsentimentosRegistrados,
    int ExportacoesUltimos30Dias,
    int SolicitacoesExclusaoAbertas);
