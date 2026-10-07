namespace EquilibraFitPlusPlus.Contracts.Admin;

/// <summary>
/// Administrative AI operation overview.
/// </summary>
public sealed record AdminIaOperacaoResponse(
    AdminIaServicoStatusResponse Servico,
    AdminIaMetricasResponse Metricas,
    IReadOnlyCollection<AdminIaHistoricoSanitizadoResponse> Historico,
    AdminIaEnsinoResponse Ensino);

/// <summary>
/// AI service status visible to administrators.
/// </summary>
public sealed record AdminIaServicoStatusResponse(
    bool Configurada,
    bool Online,
    int? LatenciaMs,
    string? BaseUrl,
    string Modelo,
    string Mensagem);

/// <summary>
/// Aggregated AI operation metrics.
/// </summary>
public sealed record AdminIaMetricasResponse(
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
public sealed record AdminIaHistoricoSanitizadoResponse(
    Guid Id,
    string Tipo,
    string Resumo,
    string Status,
    string? Modelo,
    DateTimeOffset CriadoEm);

/// <summary>
/// Published AI teaching configuration.
/// </summary>
public sealed record AdminIaEnsinoResponse(
    string Versao,
    string InstrucoesCoach,
    string RegrasAlimentacao,
    string RegrasTreino,
    string BaseConhecimento,
    string ExemplosBoasRespostas,
    bool Publicada,
    DateTimeOffset? PublicadaEm);

/// <summary>
/// Published AI teaching version summary.
/// </summary>
public sealed record AdminIaEnsinoVersaoResponse(
    string Versao,
    DateTimeOffset PublicadaEm,
    string? Observacao);

/// <summary>
/// Request used to publish AI teaching configuration.
/// </summary>
public sealed record PublicarAdminIaEnsinoRequest(
    string InstrucoesCoach,
    string RegrasAlimentacao,
    string RegrasTreino,
    string BaseConhecimento,
    string ExemplosBoasRespostas,
    string? Observacao);

/// <summary>
/// Request used to test AI teaching configuration before publishing.
/// </summary>
public sealed record TestarAdminIaEnsinoRequest(
    string Pergunta,
    string InstrucoesCoach,
    string RegrasAlimentacao,
    string RegrasTreino,
    string BaseConhecimento,
    string ExemplosBoasRespostas);

/// <summary>
/// AI teaching test response.
/// </summary>
public sealed record TestarAdminIaEnsinoResponse(
    string Resposta,
    string Modelo,
    bool UsouFallback,
    IReadOnlyCollection<string> Avisos);

/// <summary>
/// Request used to restore a published AI teaching version.
/// </summary>
public sealed record RestaurarAdminIaEnsinoRequest(string Versao, string? Observacao);
