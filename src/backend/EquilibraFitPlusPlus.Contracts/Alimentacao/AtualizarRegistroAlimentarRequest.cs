namespace EquilibraFitPlusPlus.Contracts.Alimentacao;

/// <summary>
/// Request used to update a food log.
/// </summary>
public sealed record AtualizarRegistroAlimentarRequest(
    DateTimeOffset DataHora,
    string TipoRefeicao,
    IReadOnlyCollection<ItemAlimentarRequest> Itens,
    bool ConfirmadoPeloUsuario = true);
