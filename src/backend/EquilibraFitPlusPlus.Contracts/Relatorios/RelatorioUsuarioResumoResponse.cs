namespace EquilibraFitPlusPlus.Contracts.Relatorios;

/// <summary>
/// User report summary for a date range.
/// </summary>
public sealed record RelatorioUsuarioResumoResponse(
    DateOnly Inicio,
    DateOnly Fim,
    int RefeicoesRegistradas,
    decimal CaloriasTotal,
    decimal ProteinaTotalG,
    decimal CarboidratoTotalG,
    decimal GorduraTotalG,
    int TreinosAtivos,
    decimal? PesoInicialKg,
    decimal? PesoFinalKg,
    decimal? VariacaoPesoKg,
    string Mensagem);

/// <summary>
/// Administrative report summary.
/// </summary>
public sealed record RelatorioAdminResumoResponse(
    DateOnly Inicio,
    DateOnly Fim,
    int UsuariosAtivos,
    int NovosUsuarios,
    int RefeicoesRegistradas,
    int PlanosGerados,
    int TreinosAtivos,
    int SessoesCoach,
    int ReconhecimentosImagem,
    decimal Receita,
    string Mensagem);
