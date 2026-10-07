namespace EquilibraFitPlusPlus.Contracts.Alimentacao;

/// <summary>
/// Food log response.
/// </summary>
public sealed record RegistroAlimentarResponse(
    Guid Id,
    DateTimeOffset DataHora,
    string TipoRefeicao,
    string Origem,
    decimal CaloriasTotal,
    decimal ProteinaTotalG,
    decimal CarboidratoTotalG,
    decimal GorduraTotalG,
    bool ConfirmadoPeloUsuario,
    IReadOnlyCollection<ItemAlimentarResponse> Itens,
    string Mensagem);

/// <summary>
/// Food item response.
/// </summary>
public sealed record ItemAlimentarResponse(
    Guid Id,
    string Nome,
    decimal Quantidade,
    string Unidade,
    decimal Calorias,
    decimal ProteinaG,
    decimal CarboidratoG,
    decimal GorduraG,
    string? FonteNutricional);
