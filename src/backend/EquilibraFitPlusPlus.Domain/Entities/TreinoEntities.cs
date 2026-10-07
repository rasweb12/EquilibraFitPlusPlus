using EquilibraFitPlusPlus.Domain.Common;

namespace EquilibraFitPlusPlus.Domain.Entities;

/// <summary>
/// Exercise catalog item.
/// </summary>
public sealed class Exercicio : AuditableEntity, ITenantEntity, ISoftDelete
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>Exercise name.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Main muscle group.</summary>
    public string GrupoMuscular { get; set; } = string.Empty;

    /// <summary>Difficulty level.</summary>
    public string Nivel { get; set; } = "Iniciante";

    /// <summary>Required equipment.</summary>
    public string? Equipamento { get; set; }

    /// <summary>Execution instructions.</summary>
    public string Instrucao { get; set; } = string.Empty;

    /// <inheritdoc />
    public DateTimeOffset? ExcluidoEm { get; set; }

    /// <inheritdoc />
    public Guid? ExcluidoPor { get; set; }

    /// <inheritdoc />
    public string? MotivoExclusao { get; set; }
}

/// <summary>
/// User workout plan.
/// </summary>
public sealed class TreinoUsuario : AuditableEntity, ITenantEntity, ISoftDelete
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>User identifier.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Workout title.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Workout objective.</summary>
    public string Objetivo { get; set; } = string.Empty;

    /// <summary>Weekly frequency.</summary>
    public byte FrequenciaSemanal { get; set; }

    /// <summary>Plan version for the user workout history.</summary>
    public int Versao { get; set; } = 1;

    /// <summary>Previous workout plan identifier when this plan evolved from another one.</summary>
    public Guid? TreinoAnteriorId { get; set; }

    /// <summary>Planned start date.</summary>
    public DateOnly DataInicio { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>Actual or planned finish date.</summary>
    public DateOnly? DataFim { get; set; }

    /// <summary>Expected cycle duration in weeks.</summary>
    public int DuracaoSemanas { get; set; } = 6;

    /// <summary>Training phase inside the cycle.</summary>
    public string Fase { get; set; } = "Fase 1";

    /// <summary>Reason used when the plan is finalized.</summary>
    public string? MotivoFinalizacao { get; set; }

    /// <summary>Indicates whether this workout is active.</summary>
    public bool Ativo { get; set; } = true;

    /// <summary>Workout owner.</summary>
    public Usuario? Usuario { get; set; }

    /// <summary>Previous workout plan navigation.</summary>
    public TreinoUsuario? TreinoAnterior { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? ExcluidoEm { get; set; }

    /// <inheritdoc />
    public Guid? ExcluidoPor { get; set; }

    /// <inheritdoc />
    public string? MotivoExclusao { get; set; }

    /// <summary>Workout exercises.</summary>
    public ICollection<TreinoExercicio> Exercicios { get; set; } = [];

    /// <summary>Recorded workout sessions.</summary>
    public ICollection<TreinoSessao> Sessoes { get; set; } = [];

    /// <summary>Evolution proposals generated for this plan.</summary>
    public ICollection<TreinoEvolucaoProposta> PropostasEvolucao { get; set; } = [];
}

/// <summary>
/// Exercise prescribed inside a user workout.
/// </summary>
public sealed class TreinoExercicio :
    AuditableEntity,
    ITenantEntity,
    ISoftDelete
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>User workout identifier.</summary>
    public Guid TreinoUsuarioId { get; set; }

    /// <summary>Exercise identifier.</summary>
    public Guid ExercicioId { get; set; }

    /// <summary>Execution order.</summary>
    public int Ordem { get; set; }

    /// <summary>Training day inside the weekly split.</summary>
    public byte DiaTreino { get; set; } = 1;

    /// <summary>Sets.</summary>
    public int Series { get; set; }

    /// <summary>Repetitions.</summary>
    public string Repeticoes { get; set; } = string.Empty;

    /// <summary>Rest interval in seconds.</summary>
    public int DescansoSegundos { get; set; }

    /// <summary>
    /// Target load in kilograms, when the exercise uses load.
    /// </summary>
    public decimal? CargaAlvoKg { get; set; }

    /// <summary>
    /// Target rate of perceived exertion from 1 to 10, when applicable.
    /// </summary>
    public byte? RpeAlvo { get; set; }

    /// <summary>
    /// Minimum target repetitions, when the prescription uses a repetition range.
    /// </summary>
    public int? RepeticoesMin { get; set; }

    /// <summary>
    /// Maximum target repetitions, when the prescription uses a repetition range.
    /// </summary>
    public int? RepeticoesMax { get; set; }

    /// <summary>
    /// Structured progression reason applied to this prescription.
    /// </summary>
    public string? ProgressaoMotivo { get; set; }

    /// <summary>
    /// Optional user-facing note for the prescribed exercise.
    /// </summary>
    public string? Observacao { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? ExcluidoEm { get; set; }

    /// <inheritdoc />
    public Guid? ExcluidoPor { get; set; }

    /// <inheritdoc />
    public string? MotivoExclusao { get; set; }

    /// <summary>User workout navigation.</summary>
    public TreinoUsuario? TreinoUsuario { get; set; }

    /// <summary>Exercise catalog navigation.</summary>
    public Exercicio? Exercicio { get; set; }

    /// <summary>
    /// Performed sets recorded for this prescribed exercise.
    /// </summary>
    public ICollection<TreinoSerieRealizada> SeriesRealizadas { get; set; } = [];
}

/// <summary>
/// Tracks whether a prescribed workout exercise was completed by the user on a given day.
/// </summary>
public sealed class TreinoExercicioConclusao : AuditableEntity, ITenantEntity
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>User identifier.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>User workout identifier.</summary>
    public Guid TreinoUsuarioId { get; set; }

    /// <summary>Prescribed workout exercise identifier.</summary>
    public Guid TreinoExercicioId { get; set; }

    /// <summary>Completion date.</summary>
    public DateOnly Data { get; set; }

    /// <summary>Indicates whether the exercise is completed for the selected day.</summary>
    public bool Concluido { get; set; }

    /// <summary>Completion timestamp.</summary>
    public DateTimeOffset? ConcluidoEm { get; set; }

    /// <summary>User workout navigation.</summary>
    public TreinoUsuario? TreinoUsuario { get; set; }

    /// <summary>Workout exercise navigation.</summary>
    public TreinoExercicio? TreinoExercicio { get; set; }

    /// <summary>Workout owner.</summary>
    public Usuario? Usuario { get; set; }
}

/// <summary>
/// Real workout execution session.
/// </summary>
public sealed class TreinoSessao : AuditableEntity, ITenantEntity
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>User identifier.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>User workout identifier.</summary>
    public Guid TreinoUsuarioId { get; set; }

    /// <summary>Client-generated operation identifier used to make offline retries idempotent.</summary>
    public Guid? OperationId { get; set; }

    /// <summary>Training day executed.</summary>
    public byte DiaTreino { get; set; }

    /// <summary>Session date.</summary>
    public DateOnly Data { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>Session start timestamp.</summary>
    public DateTimeOffset IniciadoEm { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Session finish timestamp.</summary>
    public DateTimeOffset? FinalizadoEm { get; set; }

    /// <summary>Optional session note.</summary>
    public string? Observacao { get; set; }

    /// <summary>Post-workout summary.</summary>
    public string? Resumo { get; set; }

    /// <summary>User workout navigation.</summary>
    public TreinoUsuario? TreinoUsuario { get; set; }

    /// <summary>Workout owner.</summary>
    public Usuario? Usuario { get; set; }

    /// <summary>Performed sets.</summary>
    public ICollection<TreinoSerieRealizada> Series { get; set; } = [];
}

/// <summary>
/// Set performed inside a real workout execution.
/// </summary>
public sealed class TreinoSerieRealizada : AuditableEntity, ITenantEntity
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>Workout session identifier.</summary>
    public Guid TreinoSessaoId { get; set; }

    /// <summary>Prescribed workout exercise identifier.</summary>
    public Guid TreinoExercicioId { get; set; }

    /// <summary>Set number inside the exercise.</summary>
    public int NumeroSerie { get; set; }

    /// <summary>Load used in kilograms.</summary>
    public decimal? CargaKg { get; set; }

    /// <summary>Actual repetitions performed.</summary>
    public int RepeticoesRealizadas { get; set; }

    /// <summary>Rate of perceived exertion from 1 to 10.</summary>
    public byte Rpe { get; set; }

    /// <summary>Optional set note.</summary>
    public string? Observacao { get; set; }

    /// <summary>Indicates whether the user reported pain or discomfort.</summary>
    public bool DorDesconforto { get; set; }

    /// <summary>Optional pain or discomfort description.</summary>
    public string? DorDescricao { get; set; }

    /// <summary>Workout session navigation.</summary>
    public TreinoSessao? Sessao { get; set; }

    /// <summary>Prescribed workout exercise navigation.</summary>
    public TreinoExercicio? TreinoExercicio { get; set; }
}

/// <summary>
/// Versioned proposal generated before evolving a workout plan.
/// </summary>
public sealed class TreinoEvolucaoProposta : AuditableEntity, ITenantEntity
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>User identifier.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Current workout plan identifier.</summary>
    public Guid TreinoUsuarioId { get; set; }

    /// <summary>Proposed new plan version.</summary>
    public int VersaoProposta { get; set; }

    /// <summary>Machine-readable proposed plan payload.</summary>
    public string PlanoJson { get; set; } = string.Empty;

    /// <summary>User-facing change summary.</summary>
    public string MudancasResumo { get; set; } = string.Empty;

    /// <summary>Current proposal status.</summary>
    public string Status { get; set; } = "Pendente";

    /// <summary>Timestamp when the proposal was applied.</summary>
    public DateTimeOffset? AplicadaEm { get; set; }

    /// <summary>User workout navigation.</summary>
    public TreinoUsuario? TreinoUsuario { get; set; }

    /// <summary>Workout owner.</summary>
    public Usuario? Usuario { get; set; }
}

/// <summary>
/// Exercise progression suggestion that can be applied or kept for later.
/// </summary>
public sealed class TreinoProgressaoSugestao : AuditableEntity, ITenantEntity
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>User identifier.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>User workout identifier.</summary>
    public Guid TreinoUsuarioId { get; set; }

    /// <summary>Prescribed workout exercise identifier.</summary>
    public Guid TreinoExercicioId { get; set; }

    /// <summary>Current load used as baseline.</summary>
    public decimal? CargaAtualKg { get; set; }

    /// <summary>Suggested next load.</summary>
    public decimal? CargaSugeridaKg { get; set; }

    /// <summary>Suggested target rate of perceived exertion from 1 to 10.</summary>
    public byte? RpeAlvo { get; set; }

    /// <summary>Suggested minimum target repetitions.</summary>
    public int? RepeticoesMin { get; set; }

    /// <summary>Suggested maximum target repetitions.</summary>
    public int? RepeticoesMax { get; set; }

    /// <summary>Suggestion explanation.</summary>
    public string Motivo { get; set; } = string.Empty;

    /// <summary>Current suggestion status.</summary>
    public string Status { get; set; } = "Pendente";

    /// <summary>Timestamp when the user decided about the suggestion.</summary>
    public DateTimeOffset? DecididaEm { get; set; }

    /// <summary>User workout navigation.</summary>
    public TreinoUsuario? TreinoUsuario { get; set; }

    /// <summary>Prescribed workout exercise navigation.</summary>
    public TreinoExercicio? TreinoExercicio { get; set; }

    /// <summary>Workout owner.</summary>
    public Usuario? Usuario { get; set; }
}
