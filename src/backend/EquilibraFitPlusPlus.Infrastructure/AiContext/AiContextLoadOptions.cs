namespace EquilibraFitPlusPlus.Infrastructure.AiContext;

internal sealed record AiContextLoadOptions(
    bool ActiveWorkout,
    bool WorkoutSessions,
    bool WorkoutSessionExercises,
    bool WorkoutCompletions,
    bool Evolution,
    bool Habits,
    bool Meals,
    bool NutritionPlan,
    bool Memories);
