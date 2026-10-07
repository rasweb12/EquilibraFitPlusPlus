namespace EquilibraFitPlusPlus.Contracts.Alimentacao;

/// <summary>
/// Request used to estimate calories and macros from a textual meal description.
/// </summary>
public sealed record EstimarRefeicaoTextoRequest(string Descricao, string? TipoRefeicao);

/// <summary>
/// Text meal estimation response.
/// </summary>
public sealed record RefeicaoTextoEstimadaResponse(
    decimal CaloriasTotal,
    decimal ProteinaTotalG,
    decimal CarboidratoTotalG,
    decimal GorduraTotalG,
    IReadOnlyCollection<ItemAlimentarResponse> Itens,
    string Modelo,
    bool UsouFallback,
    string Mensagem);
