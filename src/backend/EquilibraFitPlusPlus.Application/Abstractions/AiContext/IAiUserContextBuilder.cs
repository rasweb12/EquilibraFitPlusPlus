namespace EquilibraFitPlusPlus.Application.Abstractions.AiContext;

/// <summary>
/// Builds minimized, purpose-specific AI contexts from trusted backend data.
/// </summary>
public interface IAiUserContextBuilder
{
    /// <summary>
    /// Builds workout context for generation and progression decisions.
    /// </summary>
    Task<WorkoutAiContext> BuildWorkoutContextAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>
    /// Builds nutrition context for diet and meal decisions.
    /// </summary>
    Task<NutritionAiContext> BuildNutritionContextAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>
    /// Builds coach context for conversational personalization.
    /// </summary>
    Task<CoachAiContext> BuildCoachContextAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>
    /// Builds progress context for body evolution and adherence decisions.
    /// </summary>
    Task<ProgressAiContext> BuildProgressContextAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken);
}

/// <summary>
/// Context used by workout AI calls.
/// </summary>
public sealed record WorkoutAiContext(
    int ContextVersion,
    AiProfileContext? Perfil,
    AiGoalsContext? Objetivos,
    AiRoutineContext? Rotina,
    AiCurrentWorkoutPlanContext? PlanoAtual,
    AiWorkoutHistoryContext? HistoricoRecente,
    AiWorkoutSafetyContext? Seguranca,
    AiBodyEvolutionContext? Evolucao,
    AiRecoveryContext? Recuperacao,
    IReadOnlyCollection<AiCoachMemoryContext> PreferenciasRelevantes);

/// <summary>
/// Context used by nutrition AI calls.
/// </summary>
public sealed record NutritionAiContext(
    int ContextVersion,
    AiProfileContext? Perfil,
    AiGoalsContext? Objetivos,
    AiBodyEvolutionContext? Evolucao,
    AiRecoveryContext? Recuperacao,
    AiNutritionPlanContext? PlanoAtual,
    IReadOnlyCollection<AiRecentMealContext> AlimentacaoRecente,
    IReadOnlyCollection<string> PreferenciasAlimentares,
    IReadOnlyCollection<string> RestricoesAlimentares,
    IReadOnlyCollection<AiCoachMemoryContext> PreferenciasRelevantes);

/// <summary>
/// Context used by AI coach chat calls.
/// </summary>
public sealed record CoachAiContext(
    int ContextVersion,
    AiProfileContext? Perfil,
    AiGoalsContext? Objetivos,
    AiRoutineContext? Rotina,
    AiNutritionPlanContext? PlanoAlimentarAtual,
    AiCurrentWorkoutPlanContext? TreinoAtual,
    AiWorkoutSafetyContext? SegurancaTreino,
    AiBodyEvolutionContext? Evolucao,
    AiRecoveryContext? Recuperacao,
    IReadOnlyCollection<AiRecentMealContext> AlimentacaoRecente,
    IReadOnlyCollection<AiCoachMemoryContext> MemoriasConfirmadas);

/// <summary>
/// Context used by progress AI calls.
/// </summary>
public sealed record ProgressAiContext(
    int ContextVersion,
    AiProfileContext? Perfil,
    AiGoalsContext? Objetivos,
    AiCurrentWorkoutPlanContext? TreinoAtual,
    AiWorkoutHistoryContext? HistoricoTreino,
    AiBodyEvolutionContext? Evolucao,
    AiRecoveryContext? Recuperacao,
    IReadOnlyCollection<AiCoachMemoryContext> PreferenciasRelevantes);

/// <summary>
/// Minimized user profile.
/// </summary>
public sealed record AiProfileContext(
    int? Idade,
    string? Sexo,
    decimal? AlturaCm,
    decimal? PesoAtualKg,
    decimal? PercentualGordura,
    decimal? PercentualMassaMagra,
    string? Nivel);

/// <summary>
/// User goals relevant to the AI task.
/// </summary>
public sealed record AiGoalsContext(
    string? ObjetivoPrincipal,
    IReadOnlyCollection<string> ObjetivosSecundarios,
    IReadOnlyCollection<string> GruposMuscularesPrioritarios,
    int? FrequenciaPreferidaDiasSemana,
    int? DuracaoPreferidaMinutos);

/// <summary>
/// User routine and constraints.
/// </summary>
public sealed record AiRoutineContext(
    int? DiasPorSemana,
    int? MinutosDisponiveis,
    IReadOnlyCollection<string> Equipamentos,
    string? AmbienteTreino,
    IReadOnlyCollection<string> Limitacoes,
    IReadOnlyCollection<string> PreferenciasRelevantes);

/// <summary>
/// Current workout plan snapshot.
/// </summary>
public sealed record AiCurrentWorkoutPlanContext(
    Guid Id,
    int Versao,
    string Fase,
    int SemanaAtual,
    int DuracaoSemanas,
    int DiasPorSemana,
    IReadOnlyCollection<AiWorkoutDayShapeContext> ExerciciosPorDia,
    IReadOnlyCollection<AiPrescribedExerciseContext> ExerciciosPrescritos);

/// <summary>
/// Number of prescribed exercises for one workout day.
/// </summary>
public sealed record AiWorkoutDayShapeContext(
    int DiaTreino,
    int QuantidadeExercicios);

/// <summary>
/// Prescribed exercise data safe for AI.
/// </summary>
public sealed record AiPrescribedExerciseContext(
    Guid ExercicioId,
    string Nome,
    string GrupoMuscular,
    int DiaTreino,
    int Ordem,
    int Series,
    string Repeticoes,
    int DescansoSegundos,
    decimal? CargaAlvoKg,
    byte? RpeAlvo,
    int? RepeticoesMin,
    int? RepeticoesMax,
    string? Observacao,
    string? ProgressaoAtual);

/// <summary>
/// Recent workout execution history.
/// </summary>
public sealed record AiWorkoutHistoryContext(
    int JanelaDias,
    int Sessoes,
    int? SessoesPrevistas,
    decimal? Aderencia,
    decimal? AderenciaPercentual,
    decimal? RpeMedio,
    byte? RpeMaximo,
    decimal VolumeTotalKg,
    IReadOnlyCollection<AiWorkoutSessionContext> SessoesRecentes,
    IReadOnlyCollection<AiPerformedExerciseContext> ExerciciosRealizados,
    IReadOnlyCollection<AiExerciseProgressionContext> Progressao,
    IReadOnlyCollection<AiExerciseLoadSummaryContext> MelhoresCargas,
    IReadOnlyCollection<AiExerciseLoadSummaryContext> UltimasCargas,
    IReadOnlyCollection<string> ExerciciosFrequentementeNaoConcluidos);

/// <summary>
/// Recent workout session summary.
/// </summary>
public sealed record AiWorkoutSessionContext(
    DateOnly Data,
    int DiaTreino,
    int? DuracaoMinutos,
    bool TeveDorDesconforto,
    byte? RpeMaximo,
    decimal VolumeKg);

/// <summary>
/// Performed exercise set data.
/// </summary>
public sealed record AiPerformedExerciseContext(
    DateOnly Data,
    string Nome,
    decimal? CargaKg,
    int Repeticoes,
    byte Rpe,
    decimal? VolumeKg,
    bool DorDesconforto);

/// <summary>
/// Exercise-level progression summary.
/// </summary>
public sealed record AiExerciseProgressionContext(
    string Nome,
    decimal? CargaInicialKg,
    decimal? CargaRecenteKg,
    int? RepeticoesRecentes,
    byte? RpeRecente,
    string Tendencia);

/// <summary>
/// Exercise load summary safe for AI reasoning.
/// </summary>
public sealed record AiExerciseLoadSummaryContext(
    string Nome,
    decimal? CargaKg,
    DateOnly? Data);

/// <summary>
/// Workout safety context.
/// </summary>
public sealed record AiWorkoutSafetyContext(
    bool DorDesconfortoRecente,
    IReadOnlyCollection<string> DescricoesDorRecentes,
    IReadOnlyCollection<string> LimitacoesAtuais);

/// <summary>
/// Body evolution context.
/// </summary>
public sealed record AiBodyEvolutionContext(
    decimal? PesoAtualKg,
    string? TendenciaPeso,
    decimal? PercentualGordura,
    decimal? PercentualMassaMagra,
    DateOnly? UltimoRegistroEm,
    IReadOnlyCollection<AiBodyMeasurementContext> MedidasRelevantes);

/// <summary>
/// Recent body measurement safe for AI.
/// </summary>
public sealed record AiBodyMeasurementContext(
    string Nome,
    decimal ValorCm,
    DateOnly Data);

/// <summary>
/// Recovery and daily habit context.
/// </summary>
public sealed record AiRecoveryContext(
    decimal? SonoRecenteHoras,
    decimal? SonoMedioHoras,
    int? AguaRecenteMl,
    int? AguaMediaMl,
    decimal? AderenciaSono,
    decimal? AderenciaHidratacao,
    decimal? HumorMedio,
    bool? AlongamentoRecente,
    bool? MeditacaoRecente,
    IReadOnlyCollection<string> HabitosRelevantes);

/// <summary>
/// Confirmed memory item used for personalization.
/// </summary>
public sealed record AiCoachMemoryContext(
    string Categoria,
    string Chave,
    string Valor,
    string TipoFato,
    bool ConfirmadoPeloUsuario);

/// <summary>
/// Current nutrition plan snapshot.
/// </summary>
public sealed record AiNutritionPlanContext(
    int CaloriasDia,
    decimal ObjetivoSemanalKg,
    string FonteGeracao);

/// <summary>
/// Recent meal summary safe for AI.
/// </summary>
public sealed record AiRecentMealContext(
    DateTimeOffset DataHora,
    string TipoRefeicao,
    decimal CaloriasTotal,
    decimal ProteinaTotalG,
    decimal CarboidratoTotalG,
    decimal GorduraTotalG);
