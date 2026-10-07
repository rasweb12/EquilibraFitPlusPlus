using EquilibraFitPlusPlus.Application.Abstractions.AiCoach;

namespace EquilibraFitPlusPlus.Application.Abstractions.Admin;

/// <summary>
/// Provides AI operation data for administrative workflows.
/// </summary>
public interface IAdminIaRepository : IAiInstructionRepository
{
    /// <summary>
    /// Gets the current AI teaching configuration shown in the administrative panel.
    /// </summary>
    Task<AdminIaEnsinoSourceData> ObterEnsinoAtualAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets aggregated AI metrics.
    /// </summary>
    Task<AdminIaMetricasSourceData> ObterMetricasAsync(Guid tenantId, DateOnly dataReferencia, CancellationToken cancellationToken);

    /// <summary>
    /// Gets sanitized recent AI history.
    /// </summary>
    Task<IReadOnlyCollection<AdminIaHistoricoSourceData>> ListarHistoricoAsync(Guid tenantId, int quantidade, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a published teaching version.
    /// </summary>
    Task<AdminIaEnsinoSourceData?> ObterEnsinoPorVersaoAsync(Guid tenantId, string versao, CancellationToken cancellationToken);

    /// <summary>
    /// Lists published teaching versions.
    /// </summary>
    Task<IReadOnlyCollection<AdminIaEnsinoVersaoSourceData>> ListarVersoesEnsinoAsync(Guid tenantId, int quantidade, CancellationToken cancellationToken);

    /// <summary>
    /// Saves the currently published AI teaching configuration.
    /// </summary>
    Task SalvarEnsinoPublicadoAsync(Guid tenantId, AdminIaEnsinoSourceData ensino, CancellationToken cancellationToken);
}

/// <summary>
/// Aggregated AI operation metrics.
/// </summary>
public sealed record AdminIaMetricasSourceData(
    int SessoesCoachHoje,
    int MensagensCoachHoje,
    int ReconhecimentosImagemHoje,
    int PlanosGeradosHoje,
    int TreinosGeradosHoje,
    int FallbacksHoje,
    int RespostasBloqueadas30Dias);

/// <summary>
/// Sanitized AI operation history item.
/// </summary>
public sealed record AdminIaHistoricoSourceData(
    Guid Id,
    string Tipo,
    string Resumo,
    string Status,
    string? Modelo,
    DateTimeOffset CriadoEm);

/// <summary>
/// Published AI teaching source data.
/// </summary>
public sealed record AdminIaEnsinoSourceData(
    string Versao,
    string InstrucoesCoach,
    string RegrasAlimentacao,
    string RegrasTreino,
    string BaseConhecimento,
    string ExemplosBoasRespostas,
    bool Publicada,
    DateTimeOffset? PublicadaEm,
    string? Observacao);

/// <summary>
/// Published AI teaching version source data.
/// </summary>
public sealed record AdminIaEnsinoVersaoSourceData(
    string Versao,
    DateTimeOffset PublicadaEm,
    string? Observacao);
