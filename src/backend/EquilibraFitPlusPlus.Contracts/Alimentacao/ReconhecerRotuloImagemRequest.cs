namespace EquilibraFitPlusPlus.Contracts.Alimentacao;

/// <summary>
/// Request used to recognize a nutrition label from an image encoded as Base64.
/// </summary>
public sealed record ReconhecerRotuloImagemRequest(
    string ImageBase64,
    string? Contexto);

/// <summary>
/// Response with AI nutrition label recognition suggestions.
/// </summary>
public sealed record RotuloNutricionalReconhecidoResponse(
    Guid AnaliseId,
    decimal ConfiancaMedia,
    bool RequerRevisaoUsuario,
    string Modelo,
    bool UsouFallback,
    string Mensagem,
    ItemAlimentarReconhecidoResponse? Item);
