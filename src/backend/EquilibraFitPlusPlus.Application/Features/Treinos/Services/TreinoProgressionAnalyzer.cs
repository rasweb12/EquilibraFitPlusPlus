using EquilibraFitPlusPlus.Domain.Entities;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Services;

/// <summary>
/// Represents a safe workout progression suggestion.
/// </summary>
public sealed record TreinoProgressionSuggestion(
    decimal? CargaAtualKg,
    decimal? CargaSugeridaKg,
    byte? RpeAlvo,
    int? RepeticoesMin,
    int? RepeticoesMax,
    string Motivo);

/// <summary>
/// Analyzes workout execution history and suggests conservative progressions.
/// </summary>
public static class TreinoProgressionAnalyzer
{
    private const byte HighRpeThreshold = 9;

    /// <summary>
    /// Calculates total volume for performed sets.
    /// </summary>
    public static decimal CalculateVolume(IEnumerable<TreinoSerieRealizada> series)
    {
        return series.Sum(serie => (serie.CargaKg ?? 0) * serie.RepeticoesRealizadas);
    }

    /// <summary>
    /// Calculates the maximum load used.
    /// </summary>
    public static decimal? CalculateMaxLoad(IEnumerable<TreinoSerieRealizada> series)
    {
        decimal[] loads = series
            .Where(serie => serie.CargaKg.HasValue)
            .Select(serie => serie.CargaKg!.Value)
            .ToArray();

        return loads.Length == 0 ? null : loads.Max();
    }

    /// <summary>
    /// Creates a progression suggestion for an exercise when the recent history supports it.
    /// </summary>
    public static TreinoProgressionSuggestion? Suggest(TreinoExercicio exercise, IReadOnlyCollection<TreinoSerieRealizada> history)
    {
        (int Min, int Max)? range = TryReadRepetitionRange(exercise);
        if (!range.HasValue)
        {
            return null;
        }

        var recentSessions = history
            .Where(serie => serie.TreinoExercicioId == exercise.Id && serie.Sessao is not null)
            .GroupBy(serie => serie.Sessao!.Id)
            .Select(group =>
            {
                TreinoSerieRealizada[] performedSets = group.ToArray();
                decimal? maxLoad = CalculateMaxLoad(performedSets);
                return new
                {
                    Date = performedSets.Max(serie => serie.Sessao!.Data),
                    SetCount = performedSets.Length,
                    HasPain = performedSets.Any(serie => serie.DorDesconforto),
                    HasHighRpe = performedSets.Any(serie => serie.Rpe >= HighRpeThreshold),
                    BelowRange = performedSets.Any(serie => serie.RepeticoesRealizadas < range.Value.Min),
                    TopReached = performedSets.All(serie => serie.RepeticoesRealizadas >= range.Value.Max),
                    MaxLoad = maxLoad
                };
            })
            .OrderByDescending(session => session.Date)
            .Take(3)
            .ToArray();

        if (recentSessions.Any(session => session.HasPain))
        {
            return null;
        }

        var evidence = recentSessions.Take(2).ToArray();
        int expectedSets = Math.Max(1, exercise.Series);
        bool insufficientEvidence = evidence.Length < 2 || evidence.Any(session => session.SetCount < expectedSets);
        if (insufficientEvidence || evidence.Any(session => session.BelowRange || session.HasHighRpe || !session.TopReached))
        {
            return null;
        }

        decimal currentLoad = evidence
            .Where(session => session.MaxLoad.HasValue)
            .Select(session => session.MaxLoad!.Value)
            .DefaultIfEmpty(0)
            .Max();

        decimal? suggestedLoad = currentLoad > 0 ? currentLoad + 2.5m : null;
        byte targetRpe = exercise.RpeAlvo ?? 8;
        string reason = suggestedLoad.HasValue
            ? $"Você atingiu o topo da faixa de repetições em duas sessões, sem desconforto e com esforço adequado. Sugestão: {currentLoad:0.##} kg → {suggestedLoad.Value:0.##} kg."
            : "Você atingiu o topo da faixa de repetições em duas sessões, sem desconforto e com esforço adequado. Sugestão: aumentar levemente a dificuldade mantendo a técnica.";

        return new TreinoProgressionSuggestion(
            currentLoad > 0 ? currentLoad : null,
            suggestedLoad,
            targetRpe,
            range.Value.Min,
            range.Value.Max,
            reason);
    }

    private static (int Min, int Max)? TryReadRepetitionRange(TreinoExercicio exercise)
    {
        if (exercise.RepeticoesMin.HasValue && exercise.RepeticoesMax.HasValue)
        {
            int min = Math.Min(exercise.RepeticoesMin.Value, exercise.RepeticoesMax.Value);
            int max = Math.Max(exercise.RepeticoesMin.Value, exercise.RepeticoesMax.Value);
            return (min, max);
        }

        return TryReadRepetitionRange(exercise.Repeticoes);
    }

    private static (int Min, int Max)? TryReadRepetitionRange(string repetitions)
    {
        if (repetitions.Contains("min", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string[] numbers = repetitions
            .Split(['-', '–', ' ', 'x', 'X'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => int.TryParse(part, out _))
            .ToArray();

        if (numbers.Length == 0)
        {
            return null;
        }

        int[] parsed = numbers.Select(int.Parse).ToArray();
        return (parsed.Min(), parsed.Max());
    }
}
