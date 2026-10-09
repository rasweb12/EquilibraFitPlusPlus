using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EquilibraFitPlusPlus.Application.Abstractions.AiCoach;
using EquilibraFitPlusPlus.Application.Abstractions.AiContext;
using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Onboarding;
using EquilibraFitPlusPlus.Application.Abstractions.Treinos;
using EquilibraFitPlusPlus.Application.Abstractions.Users;
using EquilibraFitPlusPlus.Application.Common.Serialization;
using EquilibraFitPlusPlus.Application.Features.Treinos.Mappings;
using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.GerarTreinoIa;

/// <summary>
/// Handles AI workout generation.
/// </summary>
public sealed class GerarTreinoIaCommandHandler : IRequestHandler<GerarTreinoIaCommand, Result<TreinoIaGeradoResponse>>
{
    private const string WorkoutPromptVersion = "workout-context-v1";

    private readonly ITreinoRepository _treinoRepository;
    private readonly IOnboardingRepository _onboardingRepository;
    private readonly IAiCoachRepository _aiCoachRepository;
    private readonly IAiCoachClient _aiCoachClient;
    private readonly IAiUserContextBuilder _aiUserContextBuilder;
    private readonly IAiFeatureFlagService _aiFeatureFlagService;
    private readonly IAiInstructionRepository _aiInstructionRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GerarTreinoIaCommandHandler> _logger;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public GerarTreinoIaCommandHandler(
        ITreinoRepository treinoRepository,
        IOnboardingRepository onboardingRepository,
        IAiCoachRepository aiCoachRepository,
        IAiCoachClient aiCoachClient,
        IAiUserContextBuilder aiUserContextBuilder,
        IAiFeatureFlagService aiFeatureFlagService,
        IAiInstructionRepository aiInstructionRepository,
        IUsuarioRepository usuarioRepository,
        IUnitOfWork unitOfWork,
        ILogger<GerarTreinoIaCommandHandler> logger)
    {
        _treinoRepository = treinoRepository;
        _onboardingRepository = onboardingRepository;
        _aiCoachRepository = aiCoachRepository;
        _aiCoachClient = aiCoachClient;
        _aiUserContextBuilder = aiUserContextBuilder;
        _aiFeatureFlagService = aiFeatureFlagService;
        _aiInstructionRepository = aiInstructionRepository;
        _usuarioRepository = usuarioRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<TreinoIaGeradoResponse>> Handle(GerarTreinoIaCommand command, CancellationToken cancellationToken)
    {
        if (await _usuarioRepository.ObterAtivoAsync(command.TenantId, command.UsuarioId, cancellationToken) is null)
        {
            return Result<TreinoIaGeradoResponse>.Failure(new Error("usuario.nao_encontrado", "Não foi possível localizar seu usuário ativo."));
        }

        PerfilSaude? perfil = await _onboardingRepository.ObterPorUsuarioAsync(command.TenantId, command.UsuarioId, cancellationToken);
        if (perfil is null)
        {
            return Result<TreinoIaGeradoResponse>.Failure(new Error(
                "perfil.questionario_necessario",
                "Vamos preencher o questionário inicial para gerar um treino mais seguro e personalizado."));
        }

        bool contextEnabled =
            await _aiFeatureFlagService.IsEnabledAsync(command.TenantId, AiFeatureFlagKeys.ContextEnabled, true, cancellationToken)
            && await _aiFeatureFlagService.IsEnabledAsync(command.TenantId, AiFeatureFlagKeys.WorkoutContextV2, true, cancellationToken);
        WorkoutAiContext workoutContext = contextEnabled
            ? await _aiUserContextBuilder.BuildWorkoutContextAsync(command.TenantId, command.UsuarioId, cancellationToken)
            : CreateEmptyWorkoutContext();
        string objective = string.IsNullOrWhiteSpace(command.Request.Objetivo)
            ? workoutContext.Objetivos?.ObjetivoPrincipal ?? perfil.Objetivo.ToString()
            : command.Request.Objetivo.Trim();
        string level = string.IsNullOrWhiteSpace(command.Request.Nivel) ? workoutContext.Perfil?.Nivel ?? "iniciante" : command.Request.Nivel.Trim();
        string[] limitations = Merge(workoutContext.Seguranca?.LimitacoesAtuais ?? JsonStringCollection.Deserialize(perfil.ObservacoesJson), command.Request.Limitacoes);
        string[] equipment = command.Request.Equipamentos?.Count > 0
            ? Merge([], command.Request.Equipamentos)
            : Merge(workoutContext.Rotina?.Equipamentos ?? [], null);
        string[] priorityGroups = Merge(workoutContext.Objetivos?.GruposMuscularesPrioritarios ?? [], command.Request.GruposMuscularesPrioritarios);
        int daysInput = command.Request.DiasPorSemana.HasValue
            ? command.Request.DiasPorSemana.Value
            : workoutContext.Rotina?.DiasPorSemana ?? perfil.DiasTreinoSemana;
        int daysPerWeek = Math.Clamp(daysInput, 1, 7);
        int durationWeeks = Math.Clamp(command.Request.DuracaoSemanas ?? 6, 1, 52);
        int? durationMinutes = command.Request.DuracaoMinutos is >= 10 and <= 240 ? command.Request.DuracaoMinutos : null;
        AiInstructionSet? instructions = await _aiInstructionRepository.ObterInstrucoesPublicadasAsync(command.TenantId, cancellationToken);

        Stopwatch aiStopwatch = Stopwatch.StartNew();
        Result<AiWorkoutGenerationClientReply> aiResult = await _aiCoachClient.GerarTreinoAsync(
            new AiWorkoutGenerationClientRequest(
                objective,
                level,
                daysPerWeek,
                limitations,
                equipment,
                durationMinutes,
                priorityGroups,
                instructions?.ToPromptFragment(),
                workoutContext,
                WorkoutPromptVersion),
            cancellationToken);

        if (aiResult.IsFailure && aiResult.Errors.Any(AiServiceErrors.IsInfrastructureError))
        {
            return Result<TreinoIaGeradoResponse>.Failure(aiResult.Errors);
        }

        AiWorkoutGenerationClientReply generated = aiResult.IsSuccess
            ? aiResult.Value!
            : CreateHybridWorkout(objective, level, daysPerWeek, limitations, equipment, priorityGroups, durationMinutes, workoutContext);
        aiStopwatch.Stop();
        _logger.LogInformation(
            "AI workout generation completed. PromptVersion={PromptVersion} Model={Model} Fallback={Fallback} DurationMs={DurationMs} Tokens={Tokens} Context={Context}",
            WorkoutPromptVersion,
            generated.Model,
            generated.FallbackUsed,
            aiStopwatch.ElapsedMilliseconds,
            null,
            CreateContextTelemetry(workoutContext));

        _aiCoachRepository.AdicionarExecutionLog(new AiExecutionLog
        {
            TenantId = command.TenantId,
            UsuarioIdHash = HashUserId(command.TenantId, command.UsuarioId),
            Operation = "workout_generation",
            PromptName = "workout/generation",
            PromptVersion = WorkoutPromptVersion,
            Model = generated.Model,
            ContextVersion = workoutContext.ContextVersion,
            RetrievalUsed = generated.RetrievalUsed,
            RetrievedDocumentIdsJson = generated.RetrievedDocumentIds is { Count: > 0 }
                ? JsonSerializer.Serialize(generated.RetrievedDocumentIds.Take(20).ToArray())
                : null,
            FallbackUsed = generated.FallbackUsed,
            LatencyMs = aiStopwatch.ElapsedMilliseconds,
            InputTokens = null,
            OutputTokens = null,
            EstimatedCost = null,
            Confidence = generated.Rationale.Confidence,
            SafetyResult = CreateSafetySummary(generated, workoutContext),
            CorrelationId = Activity.Current?.TraceId.ToString()
        });

        TreinoUsuario? treinoAtivo = await _treinoRepository.ObterTreinoAtivoAsync(command.TenantId, command.UsuarioId, cancellationToken);
        if (treinoAtivo is not null)
        {
            treinoAtivo.Ativo = false;
            treinoAtivo.DataFim = DateOnly.FromDateTime(DateTime.UtcNow);
            treinoAtivo.MotivoFinalizacao = "Novo plano gerado por IA.";
        }

        int nextVersion = await _treinoRepository.ObterMaiorVersaoAsync(command.TenantId, command.UsuarioId, cancellationToken) + 1;
        var treino = new TreinoUsuario
        {
            TenantId = command.TenantId,
            UsuarioId = command.UsuarioId,
            Nome = $"Treino IA - {objective}",
            Objetivo = objective,
            FrequenciaSemanal = (byte)Math.Clamp(generated.Frequency, 1, 7),
            Versao = nextVersion,
            TreinoAnteriorId = treinoAtivo?.Id,
            DataInicio = DateOnly.FromDateTime(DateTime.UtcNow),
            DuracaoSemanas = durationWeeks,
            Fase = "Fase 1",
            Ativo = true
        };

        int globalOrder = 1;
        int dayNumber = 1;
        int maxExercisesPerDay = ResolveMaxExercisesPerDay(workoutContext, durationMinutes);
        foreach (AiWorkoutDay day in generated.Days.Take(7))
        {
            int orderInDay = 1;
            foreach (string exerciseName in day.Exercises.Where(item => !string.IsNullOrWhiteSpace(item)).Take(maxExercisesPerDay))
            {
                Exercicio exercise = CreateExercise(command.TenantId, exerciseName, day.Focus, level, equipment);
                string repetitions = CreateRepetitionSuggestion(exerciseName);
                (int Min, int Max)? repetitionRange = TryReadRepetitionRange(repetitions);
                _treinoRepository.AdicionarExercicio(exercise);
                treino.Exercicios.Add(new TreinoExercicio
                {
                    TenantId = command.TenantId,
                    Exercicio = exercise,
                    ExercicioId = exercise.Id,
                    DiaTreino = (byte)dayNumber,
                    Ordem = orderInDay++,
                    Series = 3,
                    Repeticoes = repetitions,
                    DescansoSegundos = 60,
                    RpeAlvo = repetitionRange.HasValue ? (byte)7 : null,
                    RepeticoesMin = repetitionRange?.Min,
                    RepeticoesMax = repetitionRange?.Max,
                    Observacao = durationMinutes.HasValue ? $"Sessão planejada para cerca de {durationMinutes.Value} minutos." : null
                });
                globalOrder++;
            }

            dayNumber++;
        }

        if (treino.Exercicios.Count == 0 || globalOrder == 1)
        {
            return Result<TreinoIaGeradoResponse>.Failure(new Error(
                "treino.geracao_sem_exercicios",
                "Não conseguimos montar exercícios com segurança agora. Podemos tentar novamente em instantes."));
        }

        _treinoRepository.AdicionarTreino(treino);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        string[] notices = generated.SafetyNotices
            .Select(notice => notice.Message)
            .Append(generated.Progression)
            .Append("A IA orienta e educa, mas não substitui educadores físicos, médicos ou profissionais habilitados.")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Result<TreinoIaGeradoResponse>.Success(new TreinoIaGeradoResponse(
            TreinoMapper.Map(treino),
            generated.Model,
            generated.FallbackUsed,
            notices,
            "Treino gerado. Podemos adaptar carga, dias e exercícios conforme sua rotina.",
            new TreinoIaRationaleResponse(
                generated.Rationale.Recommendation,
                generated.Rationale.Reason,
                generated.Rationale.Confidence)));
    }

    private static AiWorkoutGenerationClientReply CreateHybridWorkout(
        string objective,
        string level,
        int daysPerWeek,
        IReadOnlyCollection<string> limitations,
        IReadOnlyCollection<string> equipment,
        IReadOnlyCollection<string> priorityGroups,
        int? durationMinutes,
        WorkoutAiContext? workoutContext)
    {
        string priority = priorityGroups.Count == 0 ? "Corpo inteiro" : string.Join(", ", priorityGroups.Take(2));
        int exerciseLimit = ResolveMaxExercisesPerDay(workoutContext, durationMinutes);
        AiWorkoutDay[] templates = CreateHybridTemplates(priority, equipment)
            .Select(day => new AiWorkoutDay(day.Name, day.Focus, day.Exercises.Take(exerciseLimit).ToArray()))
            .ToArray();

        return new AiWorkoutGenerationClientReply(
            daysPerWeek,
            templates.Take(daysPerWeek).ToArray(),
            CreateHybridProgression(durationMinutes, workoutContext),
            [
                new AiSafetyNotice("Dor, lesão ou condição especial pedem orientação profissional.", true),
                new AiSafetyNotice("Podemos adaptar dias e exercícios conforme rotina.", false)
            ],
            "equilibrafit-workout-hybrid-v3",
            true,
            CreateHybridRationale(workoutContext));
    }

    private static WorkoutAiContext CreateEmptyWorkoutContext()
    {
        return new WorkoutAiContext(
            1,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            []);
    }

    private static AiWorkoutDay[] CreateHybridTemplates(string priority, IReadOnlyCollection<string> equipment)
    {
        if (ContainsEquipment(equipment, "halter"))
        {
            return
            [
                new("Treino A", priority, ["Agachamento goblet", "Remada com halteres", "Supino com halteres", "Prancha"]),
                new("Treino B", "Forca e mobilidade", ["Terra romeno com halteres", "Desenvolvimento com halteres", "Ponte de quadril", "Mobilidade de quadril"]),
                new("Treino C", "Condicionamento", ["Caminhada leve", "Step baixo", "Prancha lateral", "Alongamento guiado"]),
                new("Treino D", "Membros inferiores", ["Avanco com halteres", "Agachamento goblet", "Panturrilha em pe", "Mesa flexora com elastico"]),
                new("Treino E", "Membros superiores", ["Supino com halteres", "Remada unilateral", "Elevacao lateral", "Rosca alternada"]),
                new("Treino F", "Zona leve", ["Caminhada", "Respiracao", "Mobilidade toracica", "Alongamento leve"]),
                new("Treino G", "Recuperacao ativa", ["Caminhada curta", "Mobilidade geral", "Alongamento leve", "Respiracao"])
            ];
        }

        if (ContainsEquipment(equipment, "maquina") || ContainsEquipment(equipment, "academia"))
        {
            return
            [
                new("Treino A", priority, ["Leg press", "Puxada na maquina", "Supino maquina", "Prancha"]),
                new("Treino B", "Forca e mobilidade", ["Mesa flexora", "Remada baixa", "Desenvolvimento maquina", "Mobilidade de quadril"]),
                new("Treino C", "Condicionamento", ["Bicicleta ergometrica", "Esteira leve", "Alongamento guiado", "Respiracao controlada"]),
                new("Treino D", "Membros inferiores", ["Leg press", "Cadeira extensora", "Mesa flexora", "Panturrilha"]),
                new("Treino E", "Membros superiores", ["Supino maquina", "Puxada aberta", "Remada baixa", "Elevacao lateral leve"]),
                new("Treino F", "Zona leve", ["Esteira leve", "Mobilidade toracica", "Alongamento leve", "Pausa consciente"]),
                new("Treino G", "Recuperacao ativa", ["Bicicleta leve", "Mobilidade geral", "Alongamento leve", "Respiracao"])
            ];
        }

        return
        [
            new("Treino A", priority, ["Agachamento assistido", "Remada com toalha", "Flexao adaptada", "Caminhada leve"]),
            new("Treino B", "Forca e mobilidade", ["Ponte de quadril", "Afundo assistido", "Mobilidade de quadril", "Prancha adaptada"]),
            new("Treino C", "Condicionamento", ["Caminhada", "Step baixo", "Alongamento guiado", "Respiracao controlada"]),
            new("Treino D", "Membros inferiores", ["Agachamento", "Avanco assistido", "Ponte unilateral", "Panturrilha"]),
            new("Treino E", "Membros superiores", ["Flexao inclinada", "Remada com toalha", "Prancha alta", "Elevacao lateral sem carga"]),
            new("Treino F", "Zona leve", ["Caminhada", "Mobilidade toracica", "Alongamento leve", "Pausa consciente"]),
            new("Treino G", "Recuperacao ativa", ["Caminhada curta", "Mobilidade geral", "Alongamento leve", "Respiracao"])
        ];
    }

    private static string CreateHybridProgression(int? durationMinutes, WorkoutAiContext? context)
    {
        string durationText = durationMinutes.HasValue ? $" dentro de cerca de {durationMinutes.Value} minutos" : string.Empty;
        if (HasRecentPain(context))
        {
            return $"Mantenha ou reduza carga{durationText}; dor recente tem prioridade sobre progressao.";
        }

        if (HasHighRecentRpe(context))
        {
            return $"Mantenha carga e reduza volume se necessario{durationText}; RPE recente alto bloqueia aumento automatico.";
        }

        if (HasSafeProgressionEvidence(context))
        {
            return $"Pode propor progressao leve{durationText}, usando historico de repeticoes e RPE antes de aumentar carga.";
        }

        return $"Aumente volume ou carga aos poucos{durationText}, mantendo tecnica e recuperacao.";
    }

    private static AiRecommendationRationale CreateHybridRationale(WorkoutAiContext? context)
    {
        if (HasRecentPain(context))
        {
            return new AiRecommendationRationale(
                "Manter ou reduzir carga.",
                "Ha dor ou desconforto recente no historico; seguranca tem prioridade sobre hipertrofia ou progressao generica.",
                0.86m);
        }

        if (HasHighRecentRpe(context))
        {
            return new AiRecommendationRationale(
                "Manter carga e controlar volume.",
                "O historico recente mostra RPE alto, entao aumentar carga agora seria pouco conservador.",
                0.8m);
        }

        if (HasSafeProgressionEvidence(context))
        {
            return new AiRecommendationRationale(
                "Propor progressao leve.",
                "O historico recente mostra progressao sem dor e RPE dentro da faixa segura.",
                0.78m);
        }

        return new AiRecommendationRationale(
            "Gerar treino base seguro.",
            "Fallback hibrido usou perfil, rotina e restricoes disponiveis sem inventar dados ausentes.",
            0.62m);
    }

    private static Exercicio CreateExercise(Guid tenantId, string name, string focus, string level, IReadOnlyCollection<string> equipment)
    {
        return new Exercicio
        {
            TenantId = tenantId,
            Nome = Truncate(name.Trim(), 160),
            GrupoMuscular = Truncate(string.IsNullOrWhiteSpace(focus) ? "Corpo inteiro" : focus.Trim(), 80),
            Nivel = Truncate(level, 40),
            Equipamento = equipment.Count == 0 ? null : Truncate(string.Join(", ", equipment), 120),
            Instrucao = $"Execute {name.Trim()} com controle, respiração fluida e amplitude confortável. Ajuste se houver desconforto."
        };
    }

    private static string CreateRepetitionSuggestion(string exerciseName)
    {
        string normalized = exerciseName.ToLowerInvariant();
        return normalized.Contains("caminhada", StringComparison.Ordinal)
            || normalized.Contains("bicicleta", StringComparison.Ordinal)
            || normalized.Contains("respiração", StringComparison.Ordinal)
            ? "10-20 min"
            : "8-12";
    }

    private static (int Min, int Max)? TryReadRepetitionRange(string repetitions)
    {
        if (repetitions.Contains("min", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        int[] numbers = repetitions
            .Split(['-', '–', ' ', 'x', 'X'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => int.TryParse(part, out _))
            .Select(int.Parse)
            .ToArray();

        return numbers.Length == 0 ? null : (numbers.Min(), numbers.Max());
    }

    private static int ResolveMaxExercisesPerDay(WorkoutAiContext? context, int? durationMinutes)
    {
        int? fromCurrentPlan = context?.PlanoAtual?.ExerciciosPorDia.Count > 0
            ? context.PlanoAtual.ExerciciosPorDia.Max(day => (int?)day.QuantidadeExercicios)
            : null;

        int durationLimit = durationMinutes switch
        {
            <= 25 => 3,
            <= 40 => 4,
            <= 60 => 6,
            _ => 10
        };

        return Math.Clamp(Math.Min(fromCurrentPlan ?? durationLimit, durationLimit), 1, 10);
    }

    private static bool HasRecentPain(WorkoutAiContext? context)
    {
        return context?.Seguranca?.DorDesconfortoRecente == true
            || context?.HistoricoRecente?.ExerciciosRealizados.Any(item => item.DorDesconforto) == true;
    }

    private static bool HasHighRecentRpe(WorkoutAiContext? context)
    {
        return context?.HistoricoRecente?.RpeMaximo >= 9
            || context?.HistoricoRecente?.ExerciciosRealizados.Any(item => item.Rpe >= 9) == true;
    }

    private static bool HasSafeProgressionEvidence(WorkoutAiContext? context)
    {
        return !HasRecentPain(context)
            && !HasHighRecentRpe(context)
            && context?.HistoricoRecente?.Progressao.Any(item =>
                item.Tendencia.StartsWith("subindo", StringComparison.OrdinalIgnoreCase)
                && (!item.RpeRecente.HasValue || item.RpeRecente <= 8)) == true;
    }

    private static bool ContainsEquipment(IReadOnlyCollection<string> equipment, string value)
    {
        return equipment.Any(item => item.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private static object CreateContextTelemetry(WorkoutAiContext context)
    {
        return new
        {
            HasProfile = context.Perfil is not null,
            HasCurrentWorkout = context.PlanoAtual is not null,
            CurrentWorkoutExercises = context.PlanoAtual?.ExerciciosPrescritos.Count ?? 0,
            CurrentWorkoutDays = context.PlanoAtual?.ExerciciosPorDia.Count ?? 0,
            RecentSessions = context.HistoricoRecente?.Sessoes ?? 0,
            RecentPain = context.Seguranca?.DorDesconfortoRecente ?? false,
            MaxRecentRpe = context.HistoricoRecente?.RpeMaximo,
            HasEvolution = context.Evolucao is not null,
            HasRecovery = context.Recuperacao is not null,
            MemoryCount = context.PreferenciasRelevantes.Count
        };
    }

    private static string[] Merge(IReadOnlyCollection<string>? first, IReadOnlyCollection<string>? second)
    {
        return (first ?? [])
            .Concat(second ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToArray();
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string HashUserId(Guid tenantId, Guid usuarioId)
    {
        string value = $"{tenantId:N}:{usuarioId:N}";
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash);
    }

    private static string CreateSafetySummary(AiWorkoutGenerationClientReply generated, WorkoutAiContext context)
    {
        if (HasRecentPain(context))
        {
            return "recent_pain";
        }

        if (HasHighRecentRpe(context))
        {
            return "high_rpe";
        }

        return generated.SafetyNotices.Any(notice => notice.RequiresProfessionalReview)
            ? "professional_review_notice"
            : "standard";
    }
}
