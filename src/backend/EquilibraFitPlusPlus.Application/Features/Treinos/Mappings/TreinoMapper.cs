using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Domain.Entities;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Mappings;

/// <summary>
/// Maps workout entities to API contracts.
/// </summary>
internal static class TreinoMapper
{
    /// <summary>
    /// Maps a user workout.
    /// </summary>
    public static TreinoUsuarioResponse Map(
        TreinoUsuario treino,
        IReadOnlyCollection<TreinoExercicioConclusao>? conclusoes = null,
        DateOnly? dataConclusao = null)
    {
        TreinoExercicioConclusao[] conclusoesDoDia = conclusoes?
            .Where(conclusao => !dataConclusao.HasValue || conclusao.Data == dataConclusao.Value)
            .ToArray() ?? [];
        TreinoExercicio[] exercicios = treino.Exercicios
            .OrderBy(item => item.DiaTreino)
            .ThenBy(item => item.Ordem)
            .ToArray();

        return new TreinoUsuarioResponse(
            treino.Id,
            treino.Nome,
            treino.Objetivo,
            treino.FrequenciaSemanal,
            treino.Ativo,
            exercicios
                .Select(item => new TreinoExercicioResponse(
                    item.Id,
                    item.ExercicioId,
                    item.Exercicio?.Nome ?? "Exercício",
                    item.Exercicio?.GrupoMuscular ?? "Não informado",
                    item.Exercicio?.Nivel ?? "Iniciante",
                    item.Exercicio?.Equipamento,
                    item.Exercicio?.Instrucao ?? string.Empty,
                    item.DiaTreino > 0 ? item.DiaTreino : CalculateTrainingDay(item, exercicios.Length, treino.FrequenciaSemanal),
                    conclusoesDoDia.Any(conclusao => conclusao.TreinoExercicioId == item.Id && conclusao.Concluido),
                    item.Ordem,
                    item.Observacao,
                    item.Series,
                    item.Repeticoes,
                    item.DescansoSegundos,
                    item.CargaAlvoKg,
                    item.RpeAlvo,
                    item.RepeticoesMin,
                    item.RepeticoesMax,
                    item.ProgressaoMotivo))
                .ToArray(),
            "Treino salvo. Vamos adaptar gradualmente para manter constância e evitar exageros.",
            treino.Versao,
            treino.TreinoAnteriorId,
            treino.DataInicio,
            treino.DataFim,
            treino.DuracaoSemanas,
            CalculateCurrentWeek(treino),
            treino.Fase);
    }

    /// <summary>
    /// Maps an exercise catalog item.
    /// </summary>
    public static ExercicioResponse Map(Exercicio exercicio)
    {
        return new ExercicioResponse(
            exercicio.Id,
            exercicio.Nome,
            exercicio.GrupoMuscular,
            exercicio.Nivel,
            exercicio.Equipamento,
            exercicio.Instrucao);
    }

    /// <summary>
    /// Maps a real workout session.
    /// </summary>
    public static TreinoSessaoResponse Map(TreinoSessao sessao)
    {
        TreinoSerieRealizada[] series = sessao.Series
            .OrderBy(serie => serie.TreinoExercicioId)
            .ThenBy(serie => serie.NumeroSerie)
            .ToArray();

        return new TreinoSessaoResponse(
            sessao.Id,
            sessao.TreinoUsuarioId,
            sessao.Data,
            sessao.DiaTreino,
            sessao.IniciadoEm,
            sessao.FinalizadoEm,
            sessao.Observacao,
            string.IsNullOrWhiteSpace(sessao.Resumo) ? "Treino registrado. O importante é continuar com consistência." : sessao.Resumo,
            series.Select(Map).ToArray());
    }

    /// <summary>
    /// Maps a performed set.
    /// </summary>
    public static TreinoSerieRealizadaResponse Map(TreinoSerieRealizada serie)
    {
        return new TreinoSerieRealizadaResponse(
            serie.Id,
            serie.TreinoExercicioId,
            serie.NumeroSerie,
            serie.CargaKg,
            serie.RepeticoesRealizadas,
            serie.Rpe,
            serie.Observacao,
            serie.DorDesconforto,
            serie.DorDescricao);
    }

    /// <summary>
    /// Maps a progression suggestion.
    /// </summary>
    public static TreinoProgressaoSugestaoResponse Map(TreinoProgressaoSugestao sugestao)
    {
        return new TreinoProgressaoSugestaoResponse(
            sugestao.Id,
            sugestao.TreinoUsuarioId,
            sugestao.TreinoExercicioId,
            sugestao.TreinoExercicio?.Exercicio?.Nome ?? "Exercício",
            sugestao.CargaAtualKg,
            sugestao.CargaSugeridaKg,
            sugestao.RpeAlvo,
            sugestao.RepeticoesMin,
            sugestao.RepeticoesMax,
            sugestao.Motivo,
            sugestao.Status,
            sugestao.Status.Equals("Aplicada", StringComparison.OrdinalIgnoreCase)
                ? "Progressão aplicada. Vamos seguir com atenção à técnica."
                : "Sugestão registrada. Você escolhe o ritmo.");
    }

    private static int CalculateTrainingDay(TreinoExercicio exercicio, int totalExercises, byte weeklyFrequency)
    {
        int frequency = Math.Clamp(weeklyFrequency, (byte)1, (byte)7);
        int exercisesPerDay = Math.Max(1, (int)Math.Ceiling(totalExercises / (double)frequency));
        int day = ((Math.Max(1, exercicio.Ordem) - 1) / exercisesPerDay) + 1;
        return Math.Clamp(day, 1, frequency);
    }

    private static int CalculateCurrentWeek(TreinoUsuario treino)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (today < treino.DataInicio)
        {
            return 1;
        }

        int elapsedDays = today.DayNumber - treino.DataInicio.DayNumber;
        int week = (elapsedDays / 7) + 1;
        return Math.Clamp(week, 1, Math.Max(1, treino.DuracaoSemanas));
    }
}
