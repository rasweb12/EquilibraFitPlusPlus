using EquilibraFitPlusPlus.Application.Abstractions.AiContext;
using EquilibraFitPlusPlus.Application.Common.Serialization;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Domain.Enums;
using EquilibraFitPlusPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EquilibraFitPlusPlus.Infrastructure.AiContext;

/// <summary>
/// Builds purpose-specific AI contexts from persisted user data.
/// </summary>
public sealed class AiUserContextBuilder : IAiUserContextBuilder
{
    private const int ContextVersion = 1;
    private const int WorkoutHistoryDays = 30;
    private const int RecoveryHistoryDays = 7;
    private const int MealHistoryDays = 7;

    private static readonly AiContextLoadOptions WorkoutLoadOptions = new(
        ActiveWorkout: true,
        WorkoutSessions: true,
        WorkoutSessionExercises: true,
        WorkoutCompletions: true,
        Evolution: true,
        Habits: true,
        Meals: false,
        NutritionPlan: false,
        Memories: true);

    private static readonly AiContextLoadOptions NutritionLoadOptions = new(
        ActiveWorkout: false,
        WorkoutSessions: false,
        WorkoutSessionExercises: false,
        WorkoutCompletions: false,
        Evolution: true,
        Habits: true,
        Meals: true,
        NutritionPlan: true,
        Memories: true);

    private static readonly AiContextLoadOptions CoachLoadOptions = new(
        ActiveWorkout: true,
        WorkoutSessions: true,
        WorkoutSessionExercises: false,
        WorkoutCompletions: false,
        Evolution: true,
        Habits: true,
        Meals: true,
        NutritionPlan: true,
        Memories: true);

    private static readonly AiContextLoadOptions ProgressLoadOptions = new(
        ActiveWorkout: true,
        WorkoutSessions: true,
        WorkoutSessionExercises: true,
        WorkoutCompletions: true,
        Evolution: true,
        Habits: true,
        Meals: false,
        NutritionPlan: false,
        Memories: true);

    private readonly EquilibraFitPlusPlusDbContext _dbContext;

    /// <summary>
    /// Initializes the builder.
    /// </summary>
    public AiUserContextBuilder(EquilibraFitPlusPlusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<WorkoutAiContext> BuildWorkoutContextAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken)
    {
        AiContextSnapshot snapshot = await LoadSnapshotAsync(tenantId, usuarioId, WorkoutLoadOptions, cancellationToken);
        return new WorkoutAiContext(
            ContextVersion,
            BuildProfile(snapshot),
            BuildGoals(snapshot),
            BuildRoutine(snapshot),
            BuildWorkoutPlan(snapshot),
            BuildWorkoutHistory(snapshot),
            BuildWorkoutSafety(snapshot),
            BuildBodyEvolution(snapshot),
            BuildRecovery(snapshot),
            BuildMemories(snapshot));
    }

    /// <inheritdoc />
    public async Task<NutritionAiContext> BuildNutritionContextAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken)
    {
        AiContextSnapshot snapshot = await LoadSnapshotAsync(tenantId, usuarioId, NutritionLoadOptions, cancellationToken);
        return new NutritionAiContext(
            ContextVersion,
            BuildProfile(snapshot),
            BuildGoals(snapshot),
            BuildBodyEvolution(snapshot),
            BuildRecovery(snapshot),
            BuildNutritionPlan(snapshot),
            BuildRecentMeals(snapshot),
            JsonStringCollection.Deserialize(snapshot.Perfil?.PreferenciasJson).ToArray(),
            JsonStringCollection.Deserialize(snapshot.Perfil?.RestricoesJson).ToArray(),
            BuildMemories(snapshot));
    }

    /// <inheritdoc />
    public async Task<CoachAiContext> BuildCoachContextAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken)
    {
        AiContextSnapshot snapshot = await LoadSnapshotAsync(tenantId, usuarioId, CoachLoadOptions, cancellationToken);
        return new CoachAiContext(
            ContextVersion,
            BuildProfile(snapshot),
            BuildGoals(snapshot),
            BuildRoutine(snapshot),
            BuildNutritionPlan(snapshot),
            BuildWorkoutPlan(snapshot),
            BuildWorkoutSafety(snapshot),
            BuildBodyEvolution(snapshot),
            BuildRecovery(snapshot),
            BuildRecentMeals(snapshot),
            BuildMemories(snapshot));
    }

    /// <inheritdoc />
    public async Task<ProgressAiContext> BuildProgressContextAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken)
    {
        AiContextSnapshot snapshot = await LoadSnapshotAsync(tenantId, usuarioId, ProgressLoadOptions, cancellationToken);
        return new ProgressAiContext(
            ContextVersion,
            BuildProfile(snapshot),
            BuildGoals(snapshot),
            BuildWorkoutPlan(snapshot),
            BuildWorkoutHistory(snapshot),
            BuildBodyEvolution(snapshot),
            BuildRecovery(snapshot),
            BuildMemories(snapshot));
    }

    private async Task<AiContextSnapshot> LoadSnapshotAsync(
        Guid tenantId,
        Guid usuarioId,
        AiContextLoadOptions options,
        CancellationToken cancellationToken)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly workoutStart = today.AddDays(-WorkoutHistoryDays);
        DateOnly recoveryStart = today.AddDays(-RecoveryHistoryDays);
        DateTimeOffset mealStart = DateTimeOffset.UtcNow.AddDays(-MealHistoryDays);

        PerfilSaude? profile = await _dbContext.PerfisSaude
            .AsNoTracking()
            .FirstOrDefaultAsync(
                perfil =>
                    perfil.TenantId == tenantId &&
                    perfil.UsuarioId == usuarioId,
                cancellationToken);

        TreinoUsuario? activeWorkout = null;
        if (options.ActiveWorkout)
        {
            activeWorkout = await _dbContext.TreinosUsuario
                .AsNoTracking()
                .AsSplitQuery()
                .Include(treino => treino.Exercicios)
                .ThenInclude(exercicio => exercicio.Exercicio)
                .Where(
                    treino =>
                        treino.TenantId == tenantId &&
                        treino.UsuarioId == usuarioId &&
                        treino.Ativo)
                .OrderByDescending(treino => treino.Versao)
                .ThenByDescending(treino => treino.CriadoEm)
                .FirstOrDefaultAsync(cancellationToken);
        }

        TreinoSessao[] recentSessions = [];
        if (options.WorkoutSessions)
        {
            Guid[] recentSessionIds = await _dbContext.TreinosSessoes
                .AsNoTracking()
                .Where(
                    sessao =>
                        sessao.TenantId == tenantId &&
                        sessao.UsuarioId == usuarioId &&
                        sessao.Data >= workoutStart)
                .OrderByDescending(sessao => sessao.Data)
                .ThenByDescending(sessao => sessao.CriadoEm)
                .Select(sessao => sessao.Id)
                .Take(30)
                .ToArrayAsync(cancellationToken);

            if (recentSessionIds.Length > 0)
            {
                recentSessions = options.WorkoutSessionExercises
                    ? await _dbContext.TreinosSessoes
                        .AsNoTracking()
                        .AsSplitQuery()
                        .Include(sessao => sessao.Series)
                        .ThenInclude(serie => serie.TreinoExercicio)
                        .ThenInclude(treinoExercicio => treinoExercicio!.Exercicio)
                        .Where(sessao => recentSessionIds.Contains(sessao.Id))
                        .OrderByDescending(sessao => sessao.Data)
                        .ThenByDescending(sessao => sessao.CriadoEm)
                        .ToArrayAsync(cancellationToken)
                    : await _dbContext.TreinosSessoes
                        .AsNoTracking()
                        .Include(sessao => sessao.Series)
                        .Where(sessao => recentSessionIds.Contains(sessao.Id))
                        .OrderByDescending(sessao => sessao.Data)
                        .ThenByDescending(sessao => sessao.CriadoEm)
                        .ToArrayAsync(cancellationToken);
            }
        }

        RegistroEvolucao[] evolution = options.Evolution
            ? await _dbContext.RegistrosEvolucao
                .AsNoTracking()
                .AsSplitQuery()
                .Include(registro => registro.Medidas)
                .Where(
                    registro =>
                        registro.TenantId == tenantId &&
                        registro.UsuarioId == usuarioId)
                .OrderByDescending(registro => registro.Data)
                .Take(6)
                .ToArrayAsync(cancellationToken)
            : [];

        TreinoExercicioConclusao[] recentCompletions = options.WorkoutCompletions
            ? await _dbContext.TreinosExerciciosConclusoes
                .AsNoTracking()
                .AsSplitQuery()
                .Include(conclusao => conclusao.TreinoExercicio)
                .ThenInclude(treinoExercicio => treinoExercicio!.Exercicio)
                .Where(
                    conclusao =>
                        conclusao.TenantId == tenantId &&
                        conclusao.UsuarioId == usuarioId &&
                        conclusao.Data >= workoutStart)
                .OrderByDescending(conclusao => conclusao.Data)
                .Take(120)
                .ToArrayAsync(cancellationToken)
            : [];

        RegistroHabitos[] habits = options.Habits
            ? await _dbContext.RegistrosHabitos
                .AsNoTracking()
                .Where(
                    registro =>
                        registro.TenantId == tenantId &&
                        registro.UsuarioId == usuarioId &&
                        registro.Data >= recoveryStart)
                .OrderByDescending(registro => registro.Data)
                .Take(14)
                .ToArrayAsync(cancellationToken)
            : [];

        RegistroAlimentar[] meals = options.Meals
            ? await _dbContext.RegistrosAlimentares
                .AsNoTracking()
                .Where(
                    registro =>
                        registro.TenantId == tenantId &&
                        registro.UsuarioId == usuarioId &&
                        registro.DataHora >= mealStart &&
                        registro.ConfirmadoPeloUsuario)
                .OrderByDescending(registro => registro.DataHora)
                .Take(25)
                .ToArrayAsync(cancellationToken)
            : [];

        PlanoUsuario? nutritionPlan = options.NutritionPlan
            ? await _dbContext.PlanosUsuario
                .AsNoTracking()
                .Where(
                    plano =>
                        plano.TenantId == tenantId &&
                        plano.UsuarioId == usuarioId &&
                        plano.Status == StatusPlano.Ativo)
                .OrderByDescending(plano => plano.Versao)
                .ThenByDescending(plano => plano.CriadoEm)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        AiCoachMemory[] memories = options.Memories
            ? await _dbContext.AiCoachMemories
                .AsNoTracking()
                .Where(
                    memory =>
                        memory.TenantId == tenantId &&
                        memory.UsuarioId == usuarioId &&
                        memory.TipoFato != AiCoachMemoryFactTypes.ModelInference &&
                        memory.TipoFato != AiCoachMemoryFactTypes.TemporaryContext &&
                        (
                            memory.TipoFato == AiCoachMemoryFactTypes.ExplicitFact ||
                            memory.TipoFato == AiCoachMemoryFactTypes.UserPreference ||
                            memory.ConfirmadoPeloUsuario
                        ))
                .OrderBy(memory => memory.Categoria)
                .ThenBy(memory => memory.Chave)
                .Take(50)
                .ToArrayAsync(cancellationToken)
            : [];

        return new AiContextSnapshot(
            today,
            profile,
            activeWorkout,
            recentSessions,
            recentCompletions,
            evolution,
            habits,
            meals,
            nutritionPlan,
            memories);
    }

    private static AiProfileContext? BuildProfile(AiContextSnapshot snapshot)
    {
        if (snapshot.Perfil is null)
        {
            return null;
        }

        RegistroEvolucao? latestEvolution = snapshot.Evolution.FirstOrDefault();
        return new AiProfileContext(
            CalculateAge(snapshot.Perfil.DataNascimento, snapshot.Today),
            snapshot.Perfil.SexoBiologico.ToString(),
            snapshot.Perfil.AlturaCm,
            latestEvolution?.PesoKg ?? snapshot.Perfil.PesoAtualKg,
            latestEvolution?.PercentualGordura,
            latestEvolution?.PercentualMassaMagra,
            snapshot.Perfil.NivelAtividade.ToString());
    }

    private static AiGoalsContext? BuildGoals(AiContextSnapshot snapshot)
    {
        string[] priorityGroups = snapshot.Memories
            .Where(memory => memory.Categoria == "PrioridadeTreino")
            .Select(memory => memory.Valor)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToArray();

        string? objective = snapshot.ActiveWorkout?.Objetivo ?? snapshot.Perfil?.Objetivo.ToString();
        int? preferredFrequency = snapshot.ActiveWorkout?.FrequenciaSemanal ?? snapshot.Perfil?.DiasTreinoSemana;
        int? preferredDuration = ResolvePreferredDuration(snapshot.Memories);

        return objective is null && priorityGroups.Length == 0 && preferredFrequency is null && preferredDuration is null
            ? null
            : new AiGoalsContext(objective, [], priorityGroups, preferredFrequency, preferredDuration);
    }

    private static AiRoutineContext? BuildRoutine(AiContextSnapshot snapshot)
    {
        string[] limitations = JsonStringCollection.Deserialize(snapshot.Perfil?.ObservacoesJson)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToArray();

        string[] equipment = snapshot.ActiveWorkout?.Exercicios
            .Select(item => item.Exercicio?.Equipamento)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .SelectMany(SplitListValue)
            .Concat(snapshot.Memories.Where(memory => memory.Categoria == "PreferenciaEquipamento").Select(memory => memory.Valor))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToArray()
            ?? [];

        string[] preferences = snapshot.Memories
            .Where(memory => memory.Categoria.StartsWith("Preferencia", StringComparison.OrdinalIgnoreCase))
            .Select(memory => memory.Valor)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToArray();

        int? daysPerWeek = snapshot.ActiveWorkout?.FrequenciaSemanal ?? snapshot.Perfil?.DiasTreinoSemana;
        int? preferredDuration = ResolvePreferredDuration(snapshot.Memories);
        string? environment = ResolveTrainingEnvironment(equipment, snapshot.Memories);
        return daysPerWeek is null && preferredDuration is null && equipment.Length == 0 && limitations.Length == 0 && preferences.Length == 0
            ? null
            : new AiRoutineContext(daysPerWeek, preferredDuration, equipment, environment, limitations, preferences);
    }

    private static AiCurrentWorkoutPlanContext? BuildWorkoutPlan(AiContextSnapshot snapshot)
    {
        if (snapshot.ActiveWorkout is null)
        {
            return null;
        }

        TreinoUsuario workout = snapshot.ActiveWorkout;
        int durationWeeks = Math.Max(workout.DuracaoSemanas, 1);
        int currentWeek = Math.Clamp(((snapshot.Today.DayNumber - workout.DataInicio.DayNumber) / 7) + 1, 1, durationWeeks);
        AiPrescribedExerciseContext[] exercises = workout.Exercicios
            .OrderBy(item => item.DiaTreino)
            .ThenBy(item => item.Ordem)
            .Select(item => new AiPrescribedExerciseContext(
                item.ExercicioId,
                item.Exercicio?.Nome ?? "Exercicio",
                item.Exercicio?.GrupoMuscular ?? "Corpo inteiro",
                item.DiaTreino,
                item.Ordem,
                item.Series,
                item.Repeticoes,
                item.DescansoSegundos,
                item.CargaAlvoKg,
                item.RpeAlvo,
                item.RepeticoesMin,
                item.RepeticoesMax,
                string.IsNullOrWhiteSpace(item.Observacao) ? null : item.Observacao,
                string.IsNullOrWhiteSpace(item.ProgressaoMotivo) ? null : item.ProgressaoMotivo))
            .Take(70)
            .ToArray();

        AiWorkoutDayShapeContext[] exercisesPerDay = exercises
            .GroupBy(item => item.DiaTreino)
            .OrderBy(group => group.Key)
            .Select(group => new AiWorkoutDayShapeContext(group.Key, group.Count()))
            .ToArray();

        return new AiCurrentWorkoutPlanContext(
            workout.Id,
            workout.Versao,
            workout.Fase,
            currentWeek,
            durationWeeks,
            workout.FrequenciaSemanal,
            exercisesPerDay,
            exercises);
    }

    private static AiWorkoutHistoryContext? BuildWorkoutHistory(AiContextSnapshot snapshot)
    {
        if (snapshot.RecentSessions.Length == 0)
        {
            return null;
        }

        AiWorkoutSessionContext[] sessions = snapshot.RecentSessions
            .Select(session =>
            {
                decimal volume = CalculateSessionVolume(session);
                return new AiWorkoutSessionContext(
                    session.Data,
                    session.DiaTreino,
                    CalculateDurationMinutes(session),
                    session.Series.Any(serie => serie.DorDesconforto),
                    session.Series.Count == 0 ? null : session.Series.Max(serie => (byte?)serie.Rpe),
                    volume);
            })
            .ToArray();

        AiPerformedExerciseContext[] performed = snapshot.RecentSessions
            .SelectMany(session => session.Series.Select(serie => new AiPerformedExerciseContext(
                session.Data,
                serie.TreinoExercicio?.Exercicio?.Nome ?? "Exercicio",
                serie.CargaKg,
                serie.RepeticoesRealizadas,
                serie.Rpe,
                serie.CargaKg.HasValue ? serie.CargaKg.Value * serie.RepeticoesRealizadas : null,
                serie.DorDesconforto)))
            .OrderByDescending(item => item.Data)
            .Take(120)
            .ToArray();

        AiExerciseProgressionContext[] progression = performed
            .GroupBy(item => item.Nome, StringComparer.OrdinalIgnoreCase)
            .Select(group => BuildProgression(group.OrderBy(item => item.Data).ToArray()))
            .Where(item => item is not null)
            .Select(item => item!)
            .Take(30)
            .ToArray();

        (decimal? adherence, int? expectedSessions) = CalculateAdherence(snapshot);
        byte? maxRpe = performed.Length == 0 ? null : performed.Max(item => (byte?)item.Rpe);
        decimal? averageRpe = performed.Length == 0 ? null : decimal.Round(performed.Average(item => (decimal)item.Rpe), 2);
        return new AiWorkoutHistoryContext(
            WorkoutHistoryDays,
            sessions.Length,
            expectedSessions,
            adherence,
            adherence.HasValue ? decimal.Round(adherence.Value * 100m, 2) : null,
            averageRpe,
            maxRpe,
            performed.Sum(item => item.VolumeKg ?? 0m),
            sessions,
            performed,
            progression,
            BuildBestLoads(performed),
            BuildLatestLoads(performed),
            BuildFrequentlyIncompleteExercises(snapshot));
    }

    private static AiWorkoutSafetyContext? BuildWorkoutSafety(AiContextSnapshot snapshot)
    {
        string[] limitations = JsonStringCollection.Deserialize(snapshot.Perfil?.ObservacoesJson)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToArray();

        string[] painDescriptions = snapshot.RecentSessions
            .SelectMany(session => session.Series)
            .Where(serie => serie.DorDesconforto && !string.IsNullOrWhiteSpace(serie.DorDescricao))
            .Select(serie => serie.DorDescricao!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToArray();

        bool hasPain = snapshot.RecentSessions.SelectMany(session => session.Series).Any(serie => serie.DorDesconforto);
        return hasPain || painDescriptions.Length > 0 || limitations.Length > 0
            ? new AiWorkoutSafetyContext(hasPain, painDescriptions, limitations)
            : null;
    }

    private static AiBodyEvolutionContext? BuildBodyEvolution(AiContextSnapshot snapshot)
    {
        RegistroEvolucao? latest = snapshot.Evolution.FirstOrDefault();
        if (latest is null && snapshot.Perfil is null)
        {
            return null;
        }

        return new AiBodyEvolutionContext(
            latest?.PesoKg ?? snapshot.Perfil?.PesoAtualKg,
            CalculateWeightTrend(snapshot.Evolution),
            latest?.PercentualGordura,
            latest?.PercentualMassaMagra,
            latest?.Data,
            BuildMeasurements(snapshot.Evolution));
    }

    private static AiRecoveryContext? BuildRecovery(AiContextSnapshot snapshot)
    {
        if (snapshot.Habits.Length == 0)
        {
            return null;
        }

        decimal sleepAverage = snapshot.Habits.Average(item => item.SonoHoras);
        decimal sleepTargetAverage = snapshot.Habits.Average(item => item.MetaSonoHoras);
        decimal hydrationAverage = snapshot.Habits.Average(item => (decimal)item.AguaMl);
        decimal hydrationTargetAverage = snapshot.Habits.Average(item => (decimal)item.MetaAguaMl);
        decimal moodAverage = snapshot.Habits.Average(item => (decimal)item.Humor);

        List<string> relevant = [];
        if (sleepTargetAverage > 0 && sleepAverage < sleepTargetAverage * 0.8m)
        {
            relevant.Add("sono abaixo da meta recente");
        }

        if (hydrationTargetAverage > 0 && hydrationAverage < hydrationTargetAverage * 0.8m)
        {
            relevant.Add("hidratacao abaixo da meta recente");
        }

        if (moodAverage <= 2.5m)
        {
            relevant.Add("humor recente baixo");
        }

        return new AiRecoveryContext(
            snapshot.Habits.OrderByDescending(item => item.Data).FirstOrDefault()?.SonoHoras,
            decimal.Round(sleepAverage, 2),
            snapshot.Habits.OrderByDescending(item => item.Data).FirstOrDefault()?.AguaMl,
            (int)Math.Round(hydrationAverage, MidpointRounding.AwayFromZero),
            sleepTargetAverage <= 0 ? null : decimal.Round(Math.Min(sleepAverage / sleepTargetAverage, 1m), 2),
            hydrationTargetAverage <= 0 ? null : decimal.Round(Math.Min(hydrationAverage / hydrationTargetAverage, 1m), 2),
            decimal.Round(moodAverage, 2),
            snapshot.Habits.Any(item => item.AlongamentoRealizado),
            snapshot.Habits.Any(item => item.MeditacaoRealizada),
            relevant);
    }

    private static IReadOnlyCollection<AiCoachMemoryContext> BuildMemories(AiContextSnapshot snapshot)
    {
        return snapshot.Memories
            .Select(memory => new AiCoachMemoryContext(memory.Categoria, memory.Chave, memory.Valor, memory.TipoFato, memory.ConfirmadoPeloUsuario))
            .ToArray();
    }

    private static AiNutritionPlanContext? BuildNutritionPlan(AiContextSnapshot snapshot)
    {
        return snapshot.NutritionPlan is null
            ? null
            : new AiNutritionPlanContext(snapshot.NutritionPlan.CaloriasDia, snapshot.NutritionPlan.ObjetivoSemanalKg, snapshot.NutritionPlan.FonteGeracao);
    }

    private static IReadOnlyCollection<AiRecentMealContext> BuildRecentMeals(AiContextSnapshot snapshot)
    {
        return snapshot.Meals
            .Select(meal => new AiRecentMealContext(
                meal.DataHora,
                meal.TipoRefeicao.ToString(),
                meal.CaloriasTotal,
                meal.ProteinaTotalG,
                meal.CarboidratoTotalG,
                meal.GorduraTotalG))
            .ToArray();
    }

    private static int? CalculateAge(DateOnly dateOfBirth, DateOnly today)
    {
        if (dateOfBirth.Year < 1900 || dateOfBirth > today)
        {
            return null;
        }

        int age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.AddYears(age) > today)
        {
            age--;
        }

        return age;
    }

    private static int? CalculateDurationMinutes(TreinoSessao session)
    {
        if (!session.FinalizadoEm.HasValue || session.FinalizadoEm.Value <= session.IniciadoEm)
        {
            return null;
        }

        return (int)Math.Round((session.FinalizadoEm.Value - session.IniciadoEm).TotalMinutes, MidpointRounding.AwayFromZero);
    }

    private static decimal CalculateSessionVolume(TreinoSessao session)
    {
        return session.Series.Sum(serie => (serie.CargaKg ?? 0m) * serie.RepeticoesRealizadas);
    }

    private static AiExerciseProgressionContext? BuildProgression(IReadOnlyCollection<AiPerformedExerciseContext> orderedHistory)
    {
        if (orderedHistory.Count == 0)
        {
            return null;
        }

        AiPerformedExerciseContext first = orderedHistory.First();
        AiPerformedExerciseContext latest = orderedHistory.Last();
        string tendency = "estavel";

        if (first.CargaKg.HasValue && latest.CargaKg.HasValue)
        {
            decimal difference = latest.CargaKg.Value - first.CargaKg.Value;
            tendency = difference > 0.5m ? "subindo" : difference < -0.5m ? "descendo" : "estavel";
        }
        else if (latest.Repeticoes > first.Repeticoes)
        {
            tendency = "subindo_repeticoes";
        }
        else if (latest.Repeticoes < first.Repeticoes)
        {
            tendency = "descendo_repeticoes";
        }

        return new AiExerciseProgressionContext(
            latest.Nome,
            first.CargaKg,
            latest.CargaKg,
            latest.Repeticoes,
            latest.Rpe,
            tendency);
    }

    private static (decimal? Adherence, int? ExpectedSessions) CalculateAdherence(AiContextSnapshot snapshot)
    {
        int weeklyFrequency = snapshot.ActiveWorkout?.FrequenciaSemanal ?? snapshot.Perfil?.DiasTreinoSemana ?? 0;
        if (weeklyFrequency <= 0)
        {
            return (null, null);
        }

        decimal expectedSessions = Math.Ceiling(WorkoutHistoryDays / 7m * weeklyFrequency);
        if (expectedSessions <= 0)
        {
            return (null, null);
        }

        return (decimal.Round(Math.Min(snapshot.RecentSessions.Length / expectedSessions, 1m), 2), (int)expectedSessions);
    }

    private static string? CalculateWeightTrend(IReadOnlyCollection<RegistroEvolucao> evolution)
    {
        if (evolution.Count < 2)
        {
            return null;
        }

        RegistroEvolucao latest = evolution.OrderByDescending(item => item.Data).First();
        RegistroEvolucao oldest = evolution.OrderBy(item => item.Data).First();
        decimal difference = latest.PesoKg - oldest.PesoKg;

        return difference > 0.5m ? "subindo" : difference < -0.5m ? "descendo" : "estavel";
    }

    private static IEnumerable<string> SplitListValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static int? ResolvePreferredDuration(IReadOnlyCollection<AiCoachMemory> memories)
    {
        return null;
    }

    private static string? ResolveTrainingEnvironment(IReadOnlyCollection<string> equipment, IReadOnlyCollection<AiCoachMemory> memories)
    {
        if (memories.Any(memory => memory.Chave.Equals("preferencia_maquinas", StringComparison.OrdinalIgnoreCase)))
        {
            return "academia";
        }

        string normalized = string.Join(" ", equipment).ToLowerInvariant();
        if (normalized.Contains("maquina", StringComparison.Ordinal) || normalized.Contains("academia", StringComparison.Ordinal))
        {
            return "academia";
        }

        return normalized.Contains("halter", StringComparison.Ordinal) ? "casa_ou_academia" : null;
    }

    private static IReadOnlyCollection<AiExerciseLoadSummaryContext> BuildBestLoads(IReadOnlyCollection<AiPerformedExerciseContext> performed)
    {
        return performed
            .Where(item => item.CargaKg.HasValue)
            .GroupBy(item => item.Nome, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                AiPerformedExerciseContext best = group
                    .OrderByDescending(item => item.CargaKg)
                    .ThenByDescending(item => item.Repeticoes)
                    .ThenByDescending(item => item.Data)
                    .First();
                return new AiExerciseLoadSummaryContext(best.Nome, best.CargaKg, best.Data);
            })
            .Take(30)
            .ToArray();
    }

    private static IReadOnlyCollection<AiExerciseLoadSummaryContext> BuildLatestLoads(IReadOnlyCollection<AiPerformedExerciseContext> performed)
    {
        return performed
            .Where(item => item.CargaKg.HasValue)
            .GroupBy(item => item.Nome, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                AiPerformedExerciseContext latest = group
                    .OrderByDescending(item => item.Data)
                    .First();
                return new AiExerciseLoadSummaryContext(latest.Nome, latest.CargaKg, latest.Data);
            })
            .Take(30)
            .ToArray();
    }

    private static IReadOnlyCollection<string> BuildFrequentlyIncompleteExercises(AiContextSnapshot snapshot)
    {
        return snapshot.RecentCompletions
            .Where(item => !item.Concluido)
            .GroupBy(item => item.TreinoExercicio?.Exercicio?.Nome ?? "Exercicio", StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() >= 2)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key)
            .Take(20)
            .ToArray();
    }

    private static IReadOnlyCollection<AiBodyMeasurementContext> BuildMeasurements(IReadOnlyCollection<RegistroEvolucao> evolution)
    {
        return evolution
            .SelectMany(registro => registro.Medidas.Select(medida => new AiBodyMeasurementContext(medida.Nome, medida.ValorCm, registro.Data)))
            .GroupBy(item => item.Nome, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(item => item.Data).First())
            .Take(20)
            .ToArray();
    }

    private sealed record AiContextSnapshot(
        DateOnly Today,
        PerfilSaude? Perfil,
        TreinoUsuario? ActiveWorkout,
        TreinoSessao[] RecentSessions,
        TreinoExercicioConclusao[] RecentCompletions,
        RegistroEvolucao[] Evolution,
        RegistroHabitos[] Habits,
        RegistroAlimentar[] Meals,
        PlanoUsuario? NutritionPlan,
        AiCoachMemory[] Memories);
}
