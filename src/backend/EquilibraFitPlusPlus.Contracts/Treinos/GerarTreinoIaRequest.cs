using System.Text.Json.Serialization;

namespace EquilibraFitPlusPlus.Contracts.Treinos;

/// <summary>
/// Optional inputs used to guide AI workout generation.
/// </summary>
[method: JsonConstructor]
public sealed record GerarTreinoIaRequest(
    string? Objetivo,
    string? Nivel,
    byte? DiasPorSemana,
    int? DuracaoMinutos,
    int? DuracaoSemanas,
    IReadOnlyCollection<string>? Limitacoes,
    IReadOnlyCollection<string>? Equipamentos,
    IReadOnlyCollection<string>? GruposMuscularesPrioritarios)
{
    /// <summary>
    /// Backward-compatible constructor for the original generation flow.
    /// </summary>
    public GerarTreinoIaRequest(
        string? nivel,
        IReadOnlyCollection<string>? limitacoes,
        IReadOnlyCollection<string>? equipamentos)
        : this(
            null,
            nivel,
            null,
            null,
            null,
            limitacoes,
            equipamentos,
            null)
    {
    }
}

/// <summary>
/// Generated workout response.
/// </summary>
public sealed record TreinoIaGeradoResponse(
    TreinoUsuarioResponse Treino,
    string ModeloIaVersao,
    bool UsouFallback,
    IReadOnlyCollection<string> Avisos,
    string Mensagem,
    TreinoIaRationaleResponse Rationale);

/// <summary>
/// Structured explanation for an AI workout recommendation.
/// </summary>
public sealed record TreinoIaRationaleResponse(
    string Recommendation,
    string Reason,
    decimal Confidence);
