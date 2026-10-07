namespace EquilibraFitPlusPlus.Contracts.Alimentacao;

/// <summary>
/// Request used to recognize a meal from an image encoded as Base64.
/// </summary>
public sealed record ReconhecerRefeicaoImagemRequest(
    string ImageBase64,
    string? TipoRefeicao,
    string? Contexto);

/// <summary>
/// Response with AI meal image recognition suggestions.
/// </summary>
public sealed record RefeicaoImagemReconhecidaResponse(
    Guid AnaliseId,
    decimal ConfiancaMedia,
    bool RequerRevisaoUsuario,
    string Modelo,
    bool UsouFallback,
    string Mensagem,
    IReadOnlyCollection<ItemAlimentarReconhecidoResponse> Itens);

/// <summary>
/// Food item estimated from a meal image.
/// </summary>
public sealed record ItemAlimentarReconhecidoResponse(
    string Nome,
    decimal Quantidade,
    string Unidade,
    decimal Calorias,
    decimal ProteinaG,
    decimal CarboidratoG,
    decimal GorduraG,
    decimal Confianca);
