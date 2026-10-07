namespace EquilibraFitPlusPlus.Contracts.Lgpd;

/// <summary>
/// LGPD data export for the authenticated user.
/// </summary>
public sealed record LgpdExportResponse(
    DateTimeOffset GeradoEm,
    LgpdUsuarioResponse Usuario,
    LgpdPerfilSaudeResponse? PerfilSaude,
    IReadOnlyCollection<LgpdConsentimentoResponse> Consentimentos,
    LgpdResumoDadosResponse ResumoDados,
    string Mensagem);

/// <summary>
/// User identity exported for LGPD access and portability.
/// </summary>
public sealed record LgpdUsuarioResponse(Guid Id, string Nome, string Email, string Role, string Status);

/// <summary>
/// Health profile exported for LGPD access and portability.
/// </summary>
public sealed record LgpdPerfilSaudeResponse(
    DateOnly DataNascimento,
    decimal AlturaCm,
    decimal PesoAtualKg,
    string Objetivo,
    string NivelAtividade,
    byte DiasTreinoSemana);

/// <summary>
/// Consent exported for LGPD access and accountability.
/// </summary>
public sealed record LgpdConsentimentoResponse(
    string Tipo,
    string Versao,
    DateTimeOffset AceitoEm,
    string Origem);

/// <summary>
/// Aggregated data counts exported for LGPD access and portability.
/// </summary>
public sealed record LgpdResumoDadosResponse(
    int RegistrosAlimentares,
    int Treinos,
    int Planos,
    int SessoesCoach,
    int Notificacoes);

/// <summary>
/// Request used to confirm account anonymization.
/// </summary>
public sealed record SolicitarExclusaoLgpdRequest(string Confirmacao);

/// <summary>
/// Administrative LGPD operational summary.
/// </summary>
public sealed record LgpdAdminResumoResponse(
    int UsuariosAtivos,
    int ConsentimentosRegistrados,
    int ExportacoesUltimos30Dias,
    int SolicitacoesExclusaoAbertas,
    string Mensagem);

/// <summary>
/// Response returned when an LGPD deletion request is recorded.
/// </summary>
public sealed record SolicitacaoExclusaoLgpdResponse(
    Guid ProtocoloId,
    DateTimeOffset RegistradoEm,
    string Mensagem);
