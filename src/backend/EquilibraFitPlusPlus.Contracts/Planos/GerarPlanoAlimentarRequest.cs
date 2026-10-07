namespace EquilibraFitPlusPlus.Contracts.Planos;

/// <summary>
/// Optional inputs used to guide diet plan generation.
/// </summary>
public sealed record GerarPlanoAlimentarRequest(
    string? Rotina,
    IReadOnlyCollection<string>? Preferencias,
    IReadOnlyCollection<string>? Restricoes);

/// <summary>
/// Generated diet plan returned to the user.
/// </summary>
public sealed record PlanoAlimentarGeradoResponse(
    Guid Id,
    int Versao,
    int CaloriasDia,
    decimal ObjetivoSemanalKg,
    string FonteGeracao,
    string? ModeloIaVersao,
    MetaNutricionalResponse MetaNutricional,
    IReadOnlyCollection<SugestaoRefeicaoResponse> SugestoesRefeicao,
    IReadOnlyCollection<string> Alternativas,
    IReadOnlyCollection<string> Avisos,
    string Mensagem);

/// <summary>
/// Nutritional target for a generated plan.
/// </summary>
public sealed record MetaNutricionalResponse(
    decimal ProteinaG,
    decimal CarboidratoG,
    decimal GorduraG,
    decimal? FibraG,
    int? AguaMl);

/// <summary>
/// Meal suggestion returned by the AI or hybrid engine.
/// </summary>
public sealed record SugestaoRefeicaoResponse(
    string Nome,
    string Descricao,
    int Calorias);
