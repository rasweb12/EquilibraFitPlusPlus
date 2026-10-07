namespace EquilibraFitPlusPlus.Application.Abstractions.AiCoach;

/// <summary>
/// Feature flag keys used by AI flows.
/// </summary>
public static class AiFeatureFlagKeys
{
    /// <summary>Enables structured backend context for AI calls.</summary>
    public const string ContextEnabled = "ai.context.enabled";

    /// <summary>Enables RAG in AI services.</summary>
    public const string RagEnabled = "ai.rag.enabled";

    /// <summary>Enables structured coach memory.</summary>
    public const string MemoryEnabled = "ai.memory.enabled";

    /// <summary>Enables AI tool calling flows.</summary>
    public const string ToolsEnabled = "ai.tools.enabled";

    /// <summary>Enables workout context v2 rollout.</summary>
    public const string WorkoutContextV2 = "ai.workout.context.v2";

    /// <summary>Enables nutrition context v2 rollout.</summary>
    public const string NutritionContextV2 = "ai.nutrition.context.v2";

    /// <summary>Enables AI eval workflows.</summary>
    public const string EvalsEnabled = "ai.evals.enabled";

    /// <summary>Enables n8n asynchronous integration hooks.</summary>
    public const string N8nEnabled = "ai.n8n.enabled";
}

/// <summary>
/// Reads tenant-scoped AI feature flags.
/// </summary>
public interface IAiFeatureFlagService
{
    /// <summary>
    /// Returns whether a feature is enabled, falling back to the supplied default when absent.
    /// </summary>
    Task<bool> IsEnabledAsync(Guid tenantId, string key, bool defaultValue, CancellationToken cancellationToken);
}
