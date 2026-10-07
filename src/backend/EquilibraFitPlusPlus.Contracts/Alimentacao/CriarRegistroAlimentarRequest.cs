namespace EquilibraFitPlusPlus.Contracts.Alimentacao;

/// <summary>
/// Request used to create a confirmed food log.
/// </summary>
public sealed record CriarRegistroAlimentarRequest(
    DateTimeOffset DataHora,
    string TipoRefeicao,
    IReadOnlyCollection<ItemAlimentarRequest> Itens,
    bool ConfirmadoPeloUsuario = true);

/// <summary>
/// Food item submitted by the user.
/// </summary>
public sealed record ItemAlimentarRequest(
    string Nome,
    decimal Quantidade,
    string Unidade,
    decimal Calorias,
    decimal ProteinaG,
    decimal CarboidratoG,
    decimal GorduraG,
    string? FonteNutricional);
