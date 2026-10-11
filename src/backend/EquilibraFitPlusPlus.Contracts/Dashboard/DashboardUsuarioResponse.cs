namespace EquilibraFitPlusPlus.Contracts.Dashboard;

/// <summary>
/// User dashboard response for the selected day.
/// </summary>
public sealed record DashboardUsuarioResponse(
    DateOnly Data,
    string MensagemApoio,
    DashboardPerfilResponse? Perfil,
    DashboardAlimentacaoResponse Alimentacao,
    DashboardTreinoResponse Treino,
    DashboardEvolucaoResponse Evolucao,
    DashboardPlanoResponse? PlanoAtual);

/// <summary>
/// Dashboard profile summary.
/// </summary>
public sealed record DashboardPerfilResponse(decimal PesoAtualKg, decimal AlturaCm, decimal Imc, string Objetivo);

/// <summary>
/// Dashboard food summary.
/// </summary>
public sealed record DashboardAlimentacaoResponse(
    decimal CaloriasConsumidas,
    decimal ProteinaG,
    decimal CarboidratoG,
    decimal GorduraG,
    int RefeicoesRegistradas,
    decimal? MetaCalorias,
    decimal? SaldoCalorico);

/// <summary>
/// Dashboard workout summary.
/// </summary>
public sealed record DashboardTreinoResponse(int TreinosAtivos, int FrequenciaSemanalPlanejada, string? ProximoTreino);

/// <summary>
/// Dashboard progress summary.
/// </summary>
public sealed record DashboardEvolucaoResponse(decimal? UltimoPesoKg, DateOnly? UltimaAtualizacao, decimal? VariacaoPesoKg);

/// <summary>
/// Dashboard active plan summary.
/// </summary>
public sealed record DashboardPlanoResponse(
    Guid Id,
    int CaloriasDia,
    string Objetivo,
    string Explicacao,
    IReadOnlyCollection<DashboardSugestaoRefeicaoResponse> SugestoesRefeicao);

/// <summary>
/// Meal suggestion shown with the active diet plan.
/// </summary>
public sealed record DashboardSugestaoRefeicaoResponse(
    string Nome,
    string Descricao,
    int Calorias,
    Guid? Id = null,
    string? ImagemUrl = null,
    IReadOnlyCollection<DashboardSubstituicaoRefeicaoResponse>? Substituicoes = null,
    DashboardReceitaRapidaResponse? ReceitaRapida = null);

/// <summary>
/// Alternative available for a persisted plan meal.
/// </summary>
public sealed record DashboardSubstituicaoRefeicaoResponse(
    int Indice,
    string Nome,
    string Descricao,
    int? Calorias,
    IReadOnlyCollection<DashboardIngredienteReceitaResponse> Ingredientes);

/// <summary>
/// Ingredient and portion disclosed by the plan generator.
/// </summary>
public sealed record DashboardIngredienteReceitaResponse(string Nome, string Quantidade);

/// <summary>
/// Quick recipe linked to the meal when generation returned enough details.
/// </summary>
public sealed record DashboardReceitaRapidaResponse(
    string Nome,
    IReadOnlyCollection<DashboardIngredienteReceitaResponse> Ingredientes,
    IReadOnlyCollection<string> Preparo,
    int TempoPreparoMinutos);
