namespace EquilibraFitPlusPlus.Infrastructure.AiCoach;

/// <summary>
/// Options used to connect the API to the external AI Coach service.
/// </summary>
public sealed class AiCoachOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "AiCoach";

    /// <summary>Base URL of the FastAPI AI service.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Relative path used to send chat requests.</summary>
    public string EndpointPath { get; set; } = "/api/v1/coach/chat";

    /// <summary>Relative path used to recognize meal images.</summary>
    public string MealRecognitionPath { get; set; } = "/api/v1/meals/recognize";

    /// <summary>Relative path used to estimate meals from text.</summary>
    public string MealTextEstimationPath { get; set; } = "/api/v1/meals/estimate-text";

    /// <summary>Relative path used to recognize nutrition label images.</summary>
    public string LabelRecognitionPath { get; set; } = "/api/v1/labels/recognize";

    /// <summary>Relative path used to generate diet plans.</summary>
    public string PlanGenerationPath { get; set; } = "/api/v1/plans/generate";

    /// <summary>Relative path used to generate workout plans.</summary>
    public string WorkoutGenerationPath { get; set; } = "/api/v1/workouts/generate";

    /// <summary>Default logical model name used for tracing and fallback responses.</summary>
    public string Model { get; set; } = "equilibrafit-coach-v1";

    /// <summary>API key provided through Key Vault or environment variables.</summary>
    public string? ApiKey { get; set; }

    /// <summary>HTTP timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 30;
}
