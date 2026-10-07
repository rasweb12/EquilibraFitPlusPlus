using EquilibraFitPlusPlus.Application.Abstractions.Treinos;
using EquilibraFitPlusPlus.Application.Features.Treinos.Services;
using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Queries.ObterHistoricoExercicio;

/// <summary>
/// Handles exercise history reads.
/// </summary>
public sealed class ObterHistoricoExercicioQueryHandler : IRequestHandler<ObterHistoricoExercicioQuery, Result<HistoricoExercicioResponse>>
{
    private readonly ITreinoRepository _treinoRepository;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public ObterHistoricoExercicioQueryHandler(ITreinoRepository treinoRepository)
    {
        _treinoRepository = treinoRepository;
    }

    /// <inheritdoc />
    public async Task<Result<HistoricoExercicioResponse>> Handle(ObterHistoricoExercicioQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<TreinoSerieRealizada> series = await _treinoRepository.ListarSeriesPorExercicioAsync(
            request.TenantId,
            request.UsuarioId,
            request.ExercicioId,
            300,
            cancellationToken);

        TreinoSerieRealizada? latest = series
            .Where(serie => serie.TreinoExercicio?.Exercicio is not null)
            .OrderByDescending(serie => serie.Sessao!.Data)
            .FirstOrDefault();

        if (latest?.TreinoExercicio?.Exercicio is null)
        {
            return Result<HistoricoExercicioResponse>.Failure(new Error("treinos.historico_nao_encontrado", "Ainda não há histórico registrado para este exercício."));
        }

        var sessions = series
            .Where(serie => serie.Sessao is not null)
            .GroupBy(serie => serie.Sessao!.Id)
            .Select(group =>
            {
                TreinoSerieRealizada[] groupSeries = group.ToArray();
                return new HistoricoExercicioSessaoResponse(
                    groupSeries.Max(serie => serie.Sessao!.Data),
                    TreinoProgressionAnalyzer.CalculateMaxLoad(groupSeries),
                    groupSeries.Sum(serie => serie.RepeticoesRealizadas),
                    TreinoProgressionAnalyzer.CalculateVolume(groupSeries),
                    groupSeries.Length == 0 ? null : (byte?)Math.Clamp((int)Math.Round(groupSeries.Average(serie => serie.Rpe)), 1, 10));
            })
            .OrderByDescending(session => session.Data)
            .Take(10)
            .ToArray();

        decimal? previousLoad = sessions.FirstOrDefault()?.MaiorCargaKg;
        decimal? maxLoad = TreinoProgressionAnalyzer.CalculateMaxLoad(series);
        decimal firstLoad = sessions.LastOrDefault(item => item.MaiorCargaKg.HasValue)?.MaiorCargaKg ?? 0;
        decimal lastLoad = sessions.FirstOrDefault(item => item.MaiorCargaKg.HasValue)?.MaiorCargaKg ?? 0;

        Exercicio exercise = latest.TreinoExercicio.Exercicio;
        var response = new HistoricoExercicioResponse(
            exercise.Id,
            exercise.Nome,
            exercise.GrupoMuscular,
            previousLoad,
            maxLoad,
            TreinoProgressionAnalyzer.CalculateVolume(series),
            lastLoad - firstLoad,
            sessions);

        return Result<HistoricoExercicioResponse>.Success(response);
    }
}
