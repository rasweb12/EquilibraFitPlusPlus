namespace EquilibraFitPlusPlus.Contracts.Gamificacao;

/// <summary>
/// User gamification summary calculated from healthy engagement.
/// </summary>
public sealed record GamificacaoResumoResponse(
    int Pontos,
    string Nivel,
    int SequenciaDias,
    IReadOnlyCollection<ConquistaResponse> Conquistas,
    string Mensagem);

/// <summary>
/// Achievement response.
/// </summary>
public sealed record ConquistaResponse(
    string Codigo,
    string Nome,
    string Descricao,
    bool Desbloqueada);

/// <summary>
/// Administrative engagement summary.
/// </summary>
public sealed record GamificacaoAdminResumoResponse(
    int UsuariosComSequencia,
    int ConquistasDesbloqueadas,
    IReadOnlyCollection<UsuarioEngajadoResponse> UsuariosEngajados);

/// <summary>
/// Support-safe engaged user ranking item.
/// </summary>
public sealed record UsuarioEngajadoResponse(
    Guid UsuarioId,
    string Nome,
    int Pontos,
    int SequenciaDias);
