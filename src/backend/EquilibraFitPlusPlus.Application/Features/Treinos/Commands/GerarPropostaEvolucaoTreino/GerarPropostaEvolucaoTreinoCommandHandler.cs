using System.Text.Json;
using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Treinos;
using EquilibraFitPlusPlus.Application.Features.Treinos.Services;
using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.GerarPropostaEvolucaoTreino;

/// <summary>
/// Handles workout evolution proposal generation.
/// </summary>
public sealed class GerarPropostaEvolucaoTreinoCommandHandler : IRequestHandler<GerarPropostaEvolucaoTreinoCommand, Result<TreinoEvolucaoPropostaResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ITreinoRepository _treinoRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public GerarPropostaEvolucaoTreinoCommandHandler(ITreinoRepository treinoRepository, IUnitOfWork unitOfWork)
    {
        _treinoRepository = treinoRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<TreinoEvolucaoPropostaResponse>> Handle(GerarPropostaEvolucaoTreinoCommand command, CancellationToken cancellationToken)
    {
        TreinoUsuario? treino = await _treinoRepository.ObterTreinoPorIdAsync(command.TenantId, command.UsuarioId, command.TreinoId, cancellationToken);
        if (treino is null)
        {
            return Result<TreinoEvolucaoPropostaResponse>.Failure(new Error("treinos.nao_encontrado", "Treino não encontrado. Podemos atualizar a tela e tentar novamente."));
        }

        IReadOnlyCollection<TreinoSerieRealizada> history = await _treinoRepository.ListarSeriesPorTreinoAsync(
            command.TenantId,
            command.UsuarioId,
            treino.Id,
            500,
            cancellationToken);

        int nextVersion = await _treinoRepository.ObterMaiorVersaoAsync(command.TenantId, command.UsuarioId, cancellationToken) + 1;
        int totalSessions = history.Where(serie => serie.Sessao is not null).Select(serie => serie.Sessao!.Id).Distinct().Count();
        bool canAddVolume = totalSessions >= 4;

        List<string> changes = [];
        List<TreinoEvolucaoMudancaResponse> detailedChanges = [];
        var exerciseRequests = treino.Exercicios
            .OrderBy(item => item.DiaTreino)
            .ThenBy(item => item.Ordem)
            .Select(item =>
            {
                TreinoProgressionSuggestion? progression = TreinoProgressionAnalyzer.Suggest(item, history);
                int proposedSets = canAddVolume && item.Series < 5 ? item.Series + 1 : item.Series;
                string? note = item.Observacao;
                decimal? targetLoadKg = item.CargaAlvoKg;
                byte? targetRpe = item.RpeAlvo;
                int? repetitionMin = item.RepeticoesMin;
                int? repetitionMax = item.RepeticoesMax;
                string? progressionReason = item.ProgressaoMotivo;
                string? changeReason = null;

                if (progression is not null)
                {
                    targetLoadKg = progression.CargaSugeridaKg ?? targetLoadKg;
                    targetRpe = progression.RpeAlvo ?? targetRpe;
                    repetitionMin = progression.RepeticoesMin ?? repetitionMin;
                    repetitionMax = progression.RepeticoesMax ?? repetitionMax;
                    progressionReason = progression.Motivo;
                    changeReason = progression.Motivo;
                    changes.Add($"{item.Exercicio?.Nome ?? "Exercício"}: progressão sugerida.");
                }
                else if (proposedSets != item.Series)
                {
                    progressionReason = "Volume levemente maior por boa consistência recente.";
                    changeReason = progressionReason;
                    changes.Add($"{item.Exercicio?.Nome ?? "Exercício"}: {item.Series} → {proposedSets} séries.");
                }

                if (changeReason is not null)
                {
                    detailedChanges.Add(new TreinoEvolucaoMudancaResponse(
                        item.Exercicio?.Nome ?? "Exercício",
                        item.Series,
                        proposedSets,
                        item.Repeticoes,
                        item.Repeticoes,
                        progression?.CargaAtualKg ?? item.CargaAlvoKg,
                        targetLoadKg,
                        item.DescansoSegundos,
                        item.DescansoSegundos,
                        changeReason));
                }

                return new TreinoExercicioRequest(
                    item.ExercicioId,
                    null,
                    null,
                    null,
                    item.Exercicio?.Equipamento,
                    null,
                    item.Ordem,
                    item.DiaTreino,
                    proposedSets,
                    item.Repeticoes,
                    item.DescansoSegundos,
                    note,
                    targetLoadKg,
                    targetRpe,
                    repetitionMin,
                    repetitionMax,
                    progressionReason);
            })
            .ToArray();

        string summary = changes.Count == 0
            ? "Proposta mantém a estrutura atual e renova o ciclo para acompanhamento histórico."
            : string.Join(" ", changes.Take(12));

        if (!string.IsNullOrWhiteSpace(command.Request.Observacao))
        {
            summary = $"{summary} Observação do usuário: {command.Request.Observacao.Trim()}";
        }

        var proposedPlan = new CriarTreinoRequest(
            $"{treino.Nome} v{nextVersion}",
            treino.Objetivo,
            treino.FrequenciaSemanal,
            exerciseRequests,
            DateOnly.FromDateTime(DateTime.UtcNow),
            treino.DuracaoSemanas,
            $"Fase {nextVersion}");

        var proposal = new TreinoEvolucaoProposta
        {
            TenantId = command.TenantId,
            UsuarioId = command.UsuarioId,
            TreinoUsuarioId = treino.Id,
            TreinoUsuario = treino,
            VersaoProposta = nextVersion,
            PlanoJson = JsonSerializer.Serialize(proposedPlan, JsonOptions),
            MudancasResumo = summary,
            Status = "Pendente"
        };

        _treinoRepository.AdicionarPropostaEvolucao(proposal);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TreinoEvolucaoPropostaResponse>.Success(new TreinoEvolucaoPropostaResponse(
            proposal.Id,
            treino.Id,
            treino.Versao,
            proposal.VersaoProposta,
            proposal.Status,
            proposal.MudancasResumo,
            proposedPlan,
            detailedChanges,
            "Proposta criada. Revise as mudanças antes de aplicar."));
    }
}
