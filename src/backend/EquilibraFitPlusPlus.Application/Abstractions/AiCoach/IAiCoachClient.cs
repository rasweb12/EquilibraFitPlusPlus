using EquilibraFitPlusPlus.Application.Abstractions.AiContext;
using EquilibraFitPlusPlus.Shared.Results;

namespace EquilibraFitPlusPlus.Application.Abstractions.AiCoach;

/// <summary>
/// Client used to request answers from the external AI Coach service.
/// </summary>
public interface IAiCoachClient
{
    /// <summary>
    /// Sends a coach request to the external AI service.
    /// </summary>
    Task<Result<AiCoachClientReply>> EnviarAsync(AiCoachClientRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Recognizes a meal image using the AI service.
    /// </summary>
    Task<Result<AiMealRecognitionClientReply>> ReconhecerRefeicaoAsync(AiMealRecognitionClientRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Estimates calories and macros from a textual meal description using the AI service.
    /// </summary>
    Task<Result<AiMealTextEstimationClientReply>> EstimarRefeicaoTextoAsync(AiMealTextEstimationClientRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Recognizes a nutrition label image using the AI service.
    /// </summary>
    Task<Result<AiLabelRecognitionClientReply>> ReconhecerRotuloAsync(AiLabelRecognitionClientRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Generates a diet plan proposal using AI or the hybrid engine.
    /// </summary>
    Task<Result<AiPlanGenerationClientReply>> GerarPlanoAlimentarAsync(AiPlanGenerationClientRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Generates a workout proposal using AI or the hybrid engine.
    /// </summary>
    Task<Result<AiWorkoutGenerationClientReply>> GerarTreinoAsync(AiWorkoutGenerationClientRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Request sent to the external AI Coach service.
/// </summary>
public sealed record AiCoachClientRequest(
    Guid TenantId,
    Guid UsuarioId,
    Guid SessaoId,
    string Mensagem,
    string SystemPromptVersion,
    string ContextoJson,
    string? Provider = null);

/// <summary>
/// Reply returned by the external AI Coach service.
/// </summary>
public sealed record AiCoachClientReply(string Conteudo, string Modelo, bool FallbackUsed = false);

/// <summary>
/// Meal image recognition request sent to the AI service.
/// </summary>
public sealed record AiMealRecognitionClientRequest(
    string ImageBase64,
    string? MealContext);

/// <summary>
/// Text meal estimation request sent to the AI service.
/// </summary>
public sealed record AiMealTextEstimationClientRequest(
    string Description,
    string? MealType);

/// <summary>
/// Nutrition label image recognition request sent to the AI service.
/// </summary>
public sealed record AiLabelRecognitionClientRequest(
    string ImageBase64,
    string? ExtractedText,
    string? LabelContext = null);

/// <summary>
/// Recognized food item returned by the AI service.
/// </summary>
public sealed record AiRecognizedFoodItem(
    string Name,
    decimal Portion,
    string Unit,
    decimal Calories,
    decimal ProteinG,
    decimal CarbsG,
    decimal FatG,
    decimal Confidence);

/// <summary>
/// Meal image recognition response returned by the AI service.
/// </summary>
public sealed record AiMealRecognitionClientReply(
    decimal Confidence,
    IReadOnlyCollection<AiRecognizedFoodItem> Items,
    bool RequiresUserReview,
    string Model,
    bool FallbackUsed,
    string Message);

/// <summary>
/// Text meal estimation response returned by the AI service.
/// </summary>
public sealed record AiMealTextEstimationClientReply(
    IReadOnlyCollection<AiRecognizedFoodItem> Items,
    string Model,
    bool FallbackUsed,
    string Message);

/// <summary>
/// Nutrition label recognition response returned by the AI service.
/// </summary>
public sealed record AiLabelRecognitionClientReply(
    string? ServingSize,
    decimal? Calories,
    decimal? ProteinG,
    decimal? CarbsG,
    decimal? FatG,
    decimal Confidence,
    bool RequiresUserReview,
    string Model,
    bool FallbackUsed,
    string Message);

/// <summary>
/// Diet plan generation request sent to the AI service.
/// </summary>
public sealed record AiPlanGenerationClientRequest(
    string Objective,
    string? Routine,
    IReadOnlyCollection<string> Preferences,
    IReadOnlyCollection<string> Restrictions,
    int MinCalories,
    int MaxCalories,
    decimal? ProteinTargetG,
    string? OperationalGuidance = null);

/// <summary>
/// Macronutrient targets returned by the AI service.
/// </summary>
public sealed record AiMacroTargets(
    int Calories,
    decimal ProteinG,
    decimal CarbsG,
    decimal FatG);

/// <summary>
/// Meal suggestion returned by the AI service.
/// </summary>
public sealed record AiMealSuggestion(
    string Name,
    string Description,
    int Calories);

/// <summary>
/// Safety notice returned by the AI service.
/// </summary>
public sealed record AiSafetyNotice(
    string Message,
    bool RequiresProfessionalReview);

/// <summary>
/// Diet plan generation response returned by the AI service.
/// </summary>
public sealed record AiPlanGenerationClientReply(
    AiMacroTargets Targets,
    IReadOnlyCollection<AiMealSuggestion> Meals,
    string Explanation,
    IReadOnlyCollection<string> Alternatives,
    IReadOnlyCollection<AiSafetyNotice> SafetyNotices,
    string Model,
    bool FallbackUsed);

/// <summary>
/// Workout generation request sent to the AI service.
/// </summary>
public sealed record AiWorkoutGenerationClientRequest(
    string Objective,
    string Level,
    int DaysPerWeek,
    IReadOnlyCollection<string> Limitations,
    IReadOnlyCollection<string> Equipment,
    int? DurationMinutes = null,
    IReadOnlyCollection<string>? PriorityMuscleGroups = null,
    string? OperationalGuidance = null,
    WorkoutAiContext? WorkoutContext = null,
    string PromptVersion = "workout-context-v1");

/// <summary>
/// Workout day returned by the AI service.
/// </summary>
public sealed record AiWorkoutDay(
    string Name,
    string Focus,
    IReadOnlyCollection<string> Exercises);

/// <summary>
/// Workout generation response returned by the AI service.
/// </summary>
public sealed record AiWorkoutGenerationClientReply(
    int Frequency,
    IReadOnlyCollection<AiWorkoutDay> Days,
    string Progression,
    IReadOnlyCollection<AiSafetyNotice> SafetyNotices,
    string Model,
    bool FallbackUsed,
    AiRecommendationRationale Rationale,
    bool RetrievalUsed = false,
    IReadOnlyCollection<string>? RetrievedDocumentIds = null);

/// <summary>
/// Structured rationale returned by an AI recommendation.
/// </summary>
public sealed record AiRecommendationRationale(
    string Recommendation,
    string Reason,
    decimal Confidence);
