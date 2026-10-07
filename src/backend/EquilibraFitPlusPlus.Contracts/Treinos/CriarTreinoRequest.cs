using System.Text.Json.Serialization;

namespace EquilibraFitPlusPlus.Contracts.Treinos;

/// <summary>
/// Request used to create a user workout.
/// </summary>
public sealed record CriarTreinoRequest(
    string Nome,
    string Objetivo,
    byte FrequenciaSemanal,
    IReadOnlyCollection<TreinoExercicioRequest> Exercicios,
    DateOnly? DataInicio = null,
    int? DuracaoSemanas = null,
    string? Fase = null);

/// <summary>
/// Exercise prescription inside a workout.
/// </summary>
public sealed record TreinoExercicioRequest
{
    /// <summary>
    /// Initializes a workout exercise prescription.
    /// </summary>
    [JsonConstructor]
    public TreinoExercicioRequest(
        Guid? exercicioId,
        string? nome,
        string? grupoMuscular,
        string? nivel,
        string? equipamento,
        string? instrucao,
        int ordem,
        byte? diaTreino,
        int series,
        string repeticoes,
        int descansoSegundos,
        string? observacao = null,
        decimal? cargaAlvoKg = null,
        byte? rpeAlvo = null,
        int? repeticoesMin = null,
        int? repeticoesMax = null,
        string? progressaoMotivo = null)
    {
        ExercicioId = exercicioId;
        Nome = nome;
        GrupoMuscular = grupoMuscular;
        Nivel = nivel;
        Equipamento = equipamento;
        Instrucao = instrucao;
        Ordem = ordem;
        DiaTreino = diaTreino;
        Series = series;
        Repeticoes = repeticoes;
        DescansoSegundos = descansoSegundos;
        Observacao = observacao;
        CargaAlvoKg = cargaAlvoKg;
        RpeAlvo = rpeAlvo;
        RepeticoesMin = repeticoesMin;
        RepeticoesMax = repeticoesMax;
        ProgressaoMotivo = progressaoMotivo;
    }

    /// <summary>
    /// Backward-compatible constructor for callers that do not yet send the training day.
    /// </summary>
    public TreinoExercicioRequest(
        Guid? exercicioId,
        string? nome,
        string? grupoMuscular,
        string? nivel,
        string? equipamento,
        string? instrucao,
        int ordem,
        int series,
        string repeticoes,
        int descansoSegundos)
        : this(exercicioId, nome, grupoMuscular, nivel, equipamento, instrucao, ordem, null, series, repeticoes, descansoSegundos)
    {
    }

    /// <summary>Existing exercise identifier.</summary>
    public Guid? ExercicioId { get; init; }

    /// <summary>Custom exercise name when creating a new catalog item.</summary>
    public string? Nome { get; init; }

    /// <summary>Custom exercise muscle group.</summary>
    public string? GrupoMuscular { get; init; }

    /// <summary>Custom exercise level.</summary>
    public string? Nivel { get; init; }

    /// <summary>Required equipment.</summary>
    public string? Equipamento { get; init; }

    /// <summary>Execution instruction for custom exercise.</summary>
    public string? Instrucao { get; init; }

    /// <summary>Exercise order inside the day.</summary>
    public int Ordem { get; init; }

    /// <summary>Training day in the weekly split.</summary>
    public byte? DiaTreino { get; init; }

    /// <summary>Prescribed sets.</summary>
    public int Series { get; init; }

    /// <summary>Prescribed repetitions text.</summary>
    public string Repeticoes { get; init; }

    /// <summary>Rest interval in seconds.</summary>
    public int DescansoSegundos { get; init; }

    /// <summary>Free text note.</summary>
    public string? Observacao { get; init; }

    /// <summary>Target load in kilograms when applicable.</summary>
    public decimal? CargaAlvoKg { get; init; }

    /// <summary>Target RPE from 1 to 10 when applicable.</summary>
    public byte? RpeAlvo { get; init; }

    /// <summary>Minimum target repetitions when applicable.</summary>
    public int? RepeticoesMin { get; init; }

    /// <summary>Maximum target repetitions when applicable.</summary>
    public int? RepeticoesMax { get; init; }

    /// <summary>Structured reason for the current progression.</summary>
    public string? ProgressaoMotivo { get; init; }
}

/// <summary>
/// Request used to update workout cycle information.
/// </summary>
public sealed record AtualizarTreinoRequest(
    string Nome,
    string Objetivo,
    byte FrequenciaSemanal,
    DateOnly DataInicio,
    int DuracaoSemanas,
    string Fase);

/// <summary>
/// Request used to update one prescribed workout exercise.
/// </summary>
public sealed record AtualizarTreinoExercicioRequest(
    byte DiaTreino,
    int Ordem,
    int Series,
    string Repeticoes,
    int DescansoSegundos,
    string? Equipamento,
    string? Observacao,
    decimal? CargaAlvoKg = null,
    byte? RpeAlvo = null,
    int? RepeticoesMin = null,
    int? RepeticoesMax = null,
    string? ProgressaoMotivo = null);

/// <summary>
/// Request used to replace a prescribed workout exercise.
/// </summary>
public sealed record SubstituirTreinoExercicioRequest(
    Guid? NovoExercicioId,
    string? Nome,
    string? GrupoMuscular,
    string? Nivel,
    string? Equipamento,
    string? Instrucao,
    bool UsarSugestaoIa);

/// <summary>
/// Request used to generate a workout evolution proposal.
/// </summary>
public sealed record GerarPropostaEvolucaoTreinoRequest(string? Observacao);

/// <summary>
/// Request used to register a real workout session.
/// </summary>
public sealed record RegistrarSessaoTreinoRequest(
    DateOnly? Data,
    byte DiaTreino,
    string? Observacao,
    IReadOnlyCollection<RegistrarSerieTreinoRequest> Series,
    Guid? OperationId = null);

/// <summary>
/// Request used to register one performed set.
/// </summary>
public sealed record RegistrarSerieTreinoRequest(
    Guid TreinoExercicioId,
    int NumeroSerie,
    decimal? CargaKg,
    int RepeticoesRealizadas,
    byte Rpe,
    string? Observacao,
    bool DorDesconforto,
    string? DorDescricao);

/// <summary>
/// Request used to decide about a progression suggestion.
/// </summary>
public sealed record DecidirProgressaoTreinoRequest(bool Aplicar);
