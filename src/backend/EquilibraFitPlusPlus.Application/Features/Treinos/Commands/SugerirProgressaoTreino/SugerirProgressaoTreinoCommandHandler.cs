using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Treinos;
using EquilibraFitPlusPlus.Application.Features.Treinos.Mappings;
using EquilibraFitPlusPlus.Application.Features.Treinos.Services;
using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.SugerirProgressaoTreino;

/// <summary>
/// Handles workout progression suggestions.
/// </summary>
public sealed class SugerirProgressaoTreinoCommandHandler : IRequestHandler<SugerirProgressaoTreinoCommand, Result<IReadOnlyCollection<TreinoProgressaoSugestaoResponse>>>
{
    private readonly ITreinoRepository _treinoRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public SugerirProgressaoTreinoCommandHandler(ITreinoRepository treinoRepository, IUnitOfWork unitOfWork)
    {
        _treinoRepository = treinoRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyCollection<TreinoProgressaoSugestaoResponse>>> Handle(SugerirProgressaoTreinoCommand command, CancellationToken cancellationToken)
    {
        TreinoUsuario? treino = await _treinoRepository.ObterTreinoPorIdAsync(command.TenantId, command.UsuarioId, command.TreinoId, cancellationToken);
        if (treino is null)
        {
            return Result<IReadOnlyCollection<TreinoProgressaoSugestaoResponse>>.Failure(new Error("treinos.nao_encontrado", "Treino não encontrado. Podemos atualizar a tela e tentar novamente."));
        }

        IReadOnlyCollection<TreinoSerieRealizada> history = await _treinoRepository.ListarSeriesPorTreinoAsync(
            command.TenantId,
            command.UsuarioId,
            treino.Id,
            500,
            cancellationToken);

        var suggestions = new List<TreinoProgressaoSugestao>();
        foreach (TreinoExercicio exercise in treino.Exercicios)
        {
            TreinoProgressionSuggestion? suggestion = TreinoProgressionAnalyzer.Suggest(exercise, history);
            if (suggestion is null)
            {
                continue;
            }

            var entity = new TreinoProgressaoSugestao
            {
                TenantId = command.TenantId,
                UsuarioId = command.UsuarioId,
                TreinoUsuarioId = treino.Id,
                TreinoExercicioId = exercise.Id,
                TreinoExercicio = exercise,
                CargaAtualKg = suggestion.CargaAtualKg,
                CargaSugeridaKg = suggestion.CargaSugeridaKg,
                RpeAlvo = suggestion.RpeAlvo,
                RepeticoesMin = suggestion.RepeticoesMin,
                RepeticoesMax = suggestion.RepeticoesMax,
                Motivo = suggestion.Motivo,
                Status = "Pendente"
            };

            _treinoRepository.AdicionarProgressaoSugestao(entity);
            suggestions.Add(entity);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (suggestions.Count == 0)
        {
            return Result<IReadOnlyCollection<TreinoProgressaoSugestaoResponse>>.Success([]);
        }

        return Result<IReadOnlyCollection<TreinoProgressaoSugestaoResponse>>.Success(
            suggestions.Select(TreinoMapper.Map).ToArray());
    }
}
