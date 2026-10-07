using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EquilibraFitPlusPlus.Application.Abstractions.AiContext;
using EquilibraFitPlusPlus.Application.Abstractions.AiCoach;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EquilibraFitPlusPlus.Infrastructure.AiCoach;

/// <summary>
/// HTTP client used to communicate with the FastAPI AI service.
/// </summary>
public sealed class AiCoachHttpClient : IAiCoachClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly AiCoachOptions _options;
    private readonly ILogger<AiCoachHttpClient> _logger;

    /// <summary>
    /// Initializes the AI HTTP client.
    /// </summary>
    public AiCoachHttpClient(HttpClient httpClient, IOptions<AiCoachOptions> options, ILogger<AiCoachHttpClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<Result<AiCoachClientReply>> EnviarAsync(AiCoachClientRequest request, CancellationToken cancellationToken)
    {
        var payload = new AiCoachHttpRequest(
            request.TenantId,
            request.UsuarioId,
            request.SessaoId,
            request.Mensagem,
            request.SystemPromptVersion,
            request.ContextoJson,
            _options.Model);

        return PostAsync<AiCoachHttpRequest, AiCoachHttpResponse, AiCoachClientReply>(
            _options.EndpointPath,
            payload,
            response =>
            {
                string? content = response?.Conteudo ?? response?.Content;
                if (string.IsNullOrWhiteSpace(content))
                {
                    return Result<AiCoachClientReply>.Failure(CreateUnavailableError("ia.coach_indisponivel"));
                }

                string model = response?.Modelo ?? response?.Model ?? _options.Model;
                return Result<AiCoachClientReply>.Success(new AiCoachClientReply(content.Trim(), model));
            },
            "AI Coach",
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<AiMealRecognitionClientReply>> ReconhecerRefeicaoAsync(AiMealRecognitionClientRequest request, CancellationToken cancellationToken)
    {
        var payload = new AiMealRecognitionHttpRequest(request.ImageBase64, request.MealContext);

        return PostAsync<AiMealRecognitionHttpRequest, AiMealRecognitionHttpResponse, AiMealRecognitionClientReply>(
            _options.MealRecognitionPath,
            payload,
            response =>
            {
                if (response is null)
                {
                    return Result<AiMealRecognitionClientReply>.Failure(CreateUnavailableError("ia.refeicao_indisponivel"));
                }

                var items = response.Items
                    .Select(item => new AiRecognizedFoodItem(
                        item.Name ?? "alimento",
                        item.Portion,
                        item.Unit ?? "g",
                        item.Calories,
                        item.ProteinG,
                        item.CarbsG,
                        item.FatG,
                        item.Confidence))
                    .ToArray();

                return Result<AiMealRecognitionClientReply>.Success(new AiMealRecognitionClientReply(
                    response.Confidence,
                    items,
                    response.RequiresUserReview,
                    string.IsNullOrWhiteSpace(response.Model) ? "equilibrafit-vision" : response.Model,
                    response.FallbackUsed,
                    string.IsNullOrWhiteSpace(response.Message)
                        ? "Análise concluída. Revise os itens antes de salvar."
                        : response.Message));
            },
            "AI meal recognition",
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<AiMealTextEstimationClientReply>> EstimarRefeicaoTextoAsync(AiMealTextEstimationClientRequest request, CancellationToken cancellationToken)
    {
        var payload = new AiMealTextEstimationHttpRequest(request.Description, request.MealType);

        return PostAsync<AiMealTextEstimationHttpRequest, AiMealTextEstimationHttpResponse, AiMealTextEstimationClientReply>(
            _options.MealTextEstimationPath,
            payload,
            response =>
            {
                if (response is null)
                {
                    return Result<AiMealTextEstimationClientReply>.Failure(CreateUnavailableError("ia.refeicao_texto_indisponivel"));
                }

                var items = response.Items
                    .Select(item => new AiRecognizedFoodItem(
                        item.Name ?? "alimento",
                        item.Portion,
                    item.Unit ?? "porção",
                        item.Calories,
                        item.ProteinG,
                        item.CarbsG,
                        item.FatG,
                        item.Confidence))
                    .ToArray();

                return Result<AiMealTextEstimationClientReply>.Success(new AiMealTextEstimationClientReply(
                    items,
                    string.IsNullOrWhiteSpace(response.Model) ? "equilibrafit-meal-text" : response.Model,
                    response.FallbackUsed,
                    string.IsNullOrWhiteSpace(response.Message)
                        ? "Estimativa criada. Revise as porções antes de salvar."
                        : response.Message));
            },
            "AI meal text estimation",
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<AiLabelRecognitionClientReply>> ReconhecerRotuloAsync(AiLabelRecognitionClientRequest request, CancellationToken cancellationToken)
    {
        var payload = new AiLabelRecognitionHttpRequest(request.ImageBase64, request.ExtractedText);

        return PostAsync<AiLabelRecognitionHttpRequest, AiLabelRecognitionHttpResponse, AiLabelRecognitionClientReply>(
            _options.LabelRecognitionPath,
            payload,
            response =>
            {
                if (response is null)
                {
                    return Result<AiLabelRecognitionClientReply>.Failure(CreateUnavailableError("ia.rotulo_indisponivel"));
                }

                return Result<AiLabelRecognitionClientReply>.Success(new AiLabelRecognitionClientReply(
                    response.ServingSize,
                    response.Calories,
                    response.ProteinG,
                    response.CarbsG,
                    response.FatG,
                    response.Confidence,
                    response.RequiresUserReview,
                    string.IsNullOrWhiteSpace(response.Model) ? "equilibrafit-labels" : response.Model,
                    response.FallbackUsed,
                    string.IsNullOrWhiteSpace(response.Message)
                        ? "Rótulo analisado. Confirme os dados antes de salvar."
                        : response.Message));
            },
            "AI label recognition",
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<AiPlanGenerationClientReply>> GerarPlanoAlimentarAsync(AiPlanGenerationClientRequest request, CancellationToken cancellationToken)
    {
        var payload = new AiPlanGenerationHttpRequest(
            request.Objective,
            request.Routine,
            request.Preferences,
            request.Restrictions,
            request.MinCalories,
            request.MaxCalories,
            request.ProteinTargetG,
            request.OperationalGuidance);

        return PostAsync<AiPlanGenerationHttpRequest, AiPlanGenerationHttpResponse, AiPlanGenerationClientReply>(
            _options.PlanGenerationPath,
            payload,
            response =>
            {
                if (response?.Targets is null)
                {
                    return Result<AiPlanGenerationClientReply>.Failure(CreateUnavailableError("ia.plano_indisponivel"));
                }

                var targets = new AiMacroTargets(
                    response.Targets.Calories,
                    response.Targets.ProteinG,
                    response.Targets.CarbsG,
                    response.Targets.FatG);

                var meals = response.Meals
                    .Select(item => new AiMealSuggestion(item.Name ?? "Refeição", item.Description ?? "Opção flexível.", item.Calories))
                    .ToArray();

                var notices = response.SafetyNotices
                    .Select(item => new AiSafetyNotice(item.Message ?? "Proposta educacional.", item.RequiresProfessionalReview))
                    .ToArray();

                return Result<AiPlanGenerationClientReply>.Success(new AiPlanGenerationClientReply(
                    targets,
                    meals,
                    response.Explanation ?? "Plano gerado para ser ajustado conforme sua rotina.",
                    response.Alternatives,
                    notices,
                    string.IsNullOrWhiteSpace(response.Model) ? _options.Model : response.Model,
                    response.FallbackUsed));
            },
            "AI plan generation",
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<AiWorkoutGenerationClientReply>> GerarTreinoAsync(AiWorkoutGenerationClientRequest request, CancellationToken cancellationToken)
    {
        var payload = new AiWorkoutGenerationHttpRequest(
            request.Objective,
            request.Level,
            request.DaysPerWeek,
            request.Limitations,
            request.Equipment,
            request.DurationMinutes,
            request.PriorityMuscleGroups ?? [],
            request.OperationalGuidance,
            request.WorkoutContext,
            request.PromptVersion);

        return PostAsync<AiWorkoutGenerationHttpRequest, AiWorkoutGenerationHttpResponse, AiWorkoutGenerationClientReply>(
            _options.WorkoutGenerationPath,
            payload,
            response =>
            {
                if (response is null)
                {
                    return Result<AiWorkoutGenerationClientReply>.Failure(CreateUnavailableError("ia.treino_indisponivel"));
                }

                var days = response.Days
                    .Select(day => new AiWorkoutDay(day.Name ?? "Treino", day.Focus ?? "Corpo inteiro", day.Exercises))
                    .ToArray();

                var notices = response.SafetyNotices
                    .Select(item => new AiSafetyNotice(item.Message ?? "Treino educacional.", item.RequiresProfessionalReview))
                    .ToArray();
                AiRecommendationRationale rationale = response.Rationale is null
                    ? CreateDefaultRationale(response.FallbackUsed)
                    : new AiRecommendationRationale(
                        string.IsNullOrWhiteSpace(response.Rationale.Recommendation) ? "Gerar plano de treino seguro." : response.Rationale.Recommendation.Trim(),
                        string.IsNullOrWhiteSpace(response.Rationale.Reason) ? "A resposta da IA nao trouxe justificativa detalhada." : response.Rationale.Reason.Trim(),
                        ClampConfidence(response.Rationale.Confidence));

                return Result<AiWorkoutGenerationClientReply>.Success(new AiWorkoutGenerationClientReply(
                    response.Frequency,
                    days,
                    response.Progression ?? "Aumente aos poucos, respeitando técnica e recuperação.",
                    notices,
                    string.IsNullOrWhiteSpace(response.Model) ? _options.Model : response.Model,
                    response.FallbackUsed,
                    rationale,
                    response.RetrievalUsed,
                    response.RetrievedDocumentIds));
            },
            "AI workout generation",
            cancellationToken);
    }

    private async Task<Result<TReply>> PostAsync<TPayload, TResponse, TReply>(
        string path,
        TPayload payload,
        Func<TResponse?, Result<TReply>> map,
        string operationName,
        CancellationToken cancellationToken)
    {
        if (_httpClient.BaseAddress is null)
        {
            return Result<TReply>.Failure(new Error(
                "ia.servico_indisponivel",
                "O serviço de IA ainda não está configurado neste ambiente. Podemos seguir com ajustes manuais normalmente."));
        }

        try
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(path, payload, JsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string responseBody = await ReadResponseBodySnippetAsync(response, cancellationToken);
                _logger.LogWarning(
                    "{OperationName} returned HTTP {StatusCode} for {Path}. DurationMs={DurationMs}. Body: {Body}",
                    operationName,
                    (int)response.StatusCode,
                    path,
                    stopwatch.ElapsedMilliseconds,
                    responseBody);
                return Result<TReply>.Failure(CreateUnavailableError("ia.servico_indisponivel"));
            }

            TResponse? body = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken);
            Result<TReply> result = map(body);
            _logger.LogInformation(
                "{OperationName} completed. Path={Path} DurationMs={DurationMs} Success={Success}",
                operationName,
                path,
                stopwatch.ElapsedMilliseconds,
                result.IsSuccess);
            return result;
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "{OperationName} timed out.", operationName);
            return Result<TReply>.Failure(CreateUnavailableError("ia.servico_indisponivel"));
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "{OperationName} request failed.", operationName);
            return Result<TReply>.Failure(CreateUnavailableError("ia.servico_indisponivel"));
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "{OperationName} returned invalid JSON.", operationName);
            return Result<TReply>.Failure(CreateUnavailableError("ia.servico_indisponivel"));
        }
    }

    private static Error CreateUnavailableError(string code)
    {
        return new Error(code, "IA indisponível no momento. Sem problemas, podemos tentar novamente em instantes.");
    }

    private static async Task<string> ReadResponseBodySnippetAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            return body.Length <= 500 ? body : body[..500];
        }
        catch (Exception)
        {
            return "<não foi possível ler o corpo da resposta>";
        }
    }

    private static AiRecommendationRationale CreateDefaultRationale(bool fallbackUsed)
    {
        return new AiRecommendationRationale(
            "Gerar plano de treino seguro.",
            fallbackUsed
                ? "Fallback hibrido usado para manter o app funcional sem IA externa."
                : "Resposta aceita sem rationale estruturado do servico de IA.",
            fallbackUsed ? 0.55m : 0.7m);
    }

    private static decimal ClampConfidence(decimal value)
    {
        return Math.Clamp(value, 0m, 1m);
    }

    private sealed record AiCoachHttpRequest(
        [property: JsonPropertyName("tenant_id")] Guid TenantId,
        [property: JsonPropertyName("usuario_id")] Guid UsuarioId,
        [property: JsonPropertyName("sessao_id")] Guid SessaoId,
        [property: JsonPropertyName("mensagem")] string Mensagem,
        [property: JsonPropertyName("system_prompt_version")] string SystemPromptVersion,
        [property: JsonPropertyName("contexto_json")] string ContextoJson,
        [property: JsonPropertyName("model")] string Model);

    private sealed record AiCoachHttpResponse(
        [property: JsonPropertyName("conteudo")] string? Conteudo,
        [property: JsonPropertyName("modelo")] string? Modelo,
        [property: JsonPropertyName("content")] string? Content,
        [property: JsonPropertyName("model")] string? Model);

    private sealed record AiMealRecognitionHttpRequest(
        [property: JsonPropertyName("image_base64")] string ImageBase64,
        [property: JsonPropertyName("meal_context")] string? MealContext);

    private sealed record AiMealTextEstimationHttpRequest(
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("meal_type")] string? MealType);

    private sealed record AiMealRecognitionHttpResponse(
        [property: JsonPropertyName("confidence")] decimal Confidence,
        [property: JsonPropertyName("items")] IReadOnlyCollection<AiRecognizedFoodItemHttpResponse> Items,
        [property: JsonPropertyName("requires_user_review")] bool RequiresUserReview,
        [property: JsonPropertyName("model")] string? Model,
        [property: JsonPropertyName("fallback_used")] bool FallbackUsed,
        [property: JsonPropertyName("message")] string? Message);

    private sealed record AiRecognizedFoodItemHttpResponse(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("portion")] decimal Portion,
        [property: JsonPropertyName("unit")] string? Unit,
        [property: JsonPropertyName("calories")] decimal Calories,
        [property: JsonPropertyName("protein_g")] decimal ProteinG,
        [property: JsonPropertyName("carbs_g")] decimal CarbsG,
        [property: JsonPropertyName("fat_g")] decimal FatG,
        [property: JsonPropertyName("confidence")] decimal Confidence);

    private sealed record AiMealTextEstimationHttpResponse(
        [property: JsonPropertyName("items")] IReadOnlyCollection<AiRecognizedFoodItemHttpResponse> Items,
        [property: JsonPropertyName("model")] string? Model,
        [property: JsonPropertyName("fallback_used")] bool FallbackUsed,
        [property: JsonPropertyName("message")] string? Message);

    private sealed record AiLabelRecognitionHttpRequest(
        [property: JsonPropertyName("image_base64")] string ImageBase64,
        [property: JsonPropertyName("extracted_text")] string? ExtractedText);

    private sealed record AiLabelRecognitionHttpResponse(
        [property: JsonPropertyName("serving_size")] string? ServingSize,
        [property: JsonPropertyName("calories")] decimal? Calories,
        [property: JsonPropertyName("protein_g")] decimal? ProteinG,
        [property: JsonPropertyName("carbs_g")] decimal? CarbsG,
        [property: JsonPropertyName("fat_g")] decimal? FatG,
        [property: JsonPropertyName("confidence")] decimal Confidence,
        [property: JsonPropertyName("requires_user_review")] bool RequiresUserReview,
        [property: JsonPropertyName("model")] string? Model,
        [property: JsonPropertyName("fallback_used")] bool FallbackUsed,
        [property: JsonPropertyName("message")] string? Message);

    private sealed record AiPlanGenerationHttpRequest(
        [property: JsonPropertyName("objective")] string Objective,
        [property: JsonPropertyName("routine")] string? Routine,
        [property: JsonPropertyName("preferences")] IReadOnlyCollection<string> Preferences,
        [property: JsonPropertyName("restrictions")] IReadOnlyCollection<string> Restrictions,
        [property: JsonPropertyName("min_calories")] int MinCalories,
        [property: JsonPropertyName("max_calories")] int MaxCalories,
        [property: JsonPropertyName("protein_target_g")] decimal? ProteinTargetG,
        [property: JsonPropertyName("operational_guidance")] string? OperationalGuidance);

    private sealed record AiPlanGenerationHttpResponse(
        [property: JsonPropertyName("targets")] AiMacroTargetsHttpResponse? Targets,
        [property: JsonPropertyName("meals")] IReadOnlyCollection<AiMealSuggestionHttpResponse> Meals,
        [property: JsonPropertyName("explanation")] string? Explanation,
        [property: JsonPropertyName("alternatives")] IReadOnlyCollection<string> Alternatives,
        [property: JsonPropertyName("safety_notices")] IReadOnlyCollection<AiSafetyNoticeHttpResponse> SafetyNotices,
        [property: JsonPropertyName("model")] string? Model,
        [property: JsonPropertyName("fallback_used")] bool FallbackUsed);

    private sealed record AiMacroTargetsHttpResponse(
        [property: JsonPropertyName("calories")] int Calories,
        [property: JsonPropertyName("protein_g")] decimal ProteinG,
        [property: JsonPropertyName("carbs_g")] decimal CarbsG,
        [property: JsonPropertyName("fat_g")] decimal FatG);

    private sealed record AiMealSuggestionHttpResponse(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("calories")] int Calories);

    private sealed record AiSafetyNoticeHttpResponse(
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("requires_professional_review")] bool RequiresProfessionalReview);

    private sealed record AiWorkoutGenerationHttpRequest(
        [property: JsonPropertyName("objective")] string Objective,
        [property: JsonPropertyName("level")] string Level,
        [property: JsonPropertyName("days_per_week")] int DaysPerWeek,
        [property: JsonPropertyName("limitations")] IReadOnlyCollection<string> Limitations,
        [property: JsonPropertyName("equipment")] IReadOnlyCollection<string> Equipment,
        [property: JsonPropertyName("duration_minutes")] int? DurationMinutes,
        [property: JsonPropertyName("priority_muscle_groups")] IReadOnlyCollection<string> PriorityMuscleGroups,
        [property: JsonPropertyName("operational_guidance")] string? OperationalGuidance,
        [property: JsonPropertyName("workout_ai_context")] WorkoutAiContext? WorkoutContext,
        [property: JsonPropertyName("prompt_version")] string PromptVersion);

    private sealed record AiWorkoutGenerationHttpResponse(
        [property: JsonPropertyName("frequency")] int Frequency,
        [property: JsonPropertyName("days")] IReadOnlyCollection<AiWorkoutDayHttpResponse> Days,
        [property: JsonPropertyName("progression")] string? Progression,
        [property: JsonPropertyName("safety_notices")] IReadOnlyCollection<AiSafetyNoticeHttpResponse> SafetyNotices,
        [property: JsonPropertyName("model")] string? Model,
        [property: JsonPropertyName("fallback_used")] bool FallbackUsed,
        [property: JsonPropertyName("rationale")] AiRecommendationRationaleHttpResponse? Rationale,
        [property: JsonPropertyName("retrieval_used")] bool RetrievalUsed,
        [property: JsonPropertyName("retrieved_document_ids")] IReadOnlyCollection<string>? RetrievedDocumentIds);

    private sealed record AiWorkoutDayHttpResponse(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("focus")] string? Focus,
        [property: JsonPropertyName("exercises")] IReadOnlyCollection<string> Exercises);

    private sealed record AiRecommendationRationaleHttpResponse(
        [property: JsonPropertyName("recommendation")] string? Recommendation,
        [property: JsonPropertyName("reason")] string? Reason,
        [property: JsonPropertyName("confidence")] decimal Confidence);
}
