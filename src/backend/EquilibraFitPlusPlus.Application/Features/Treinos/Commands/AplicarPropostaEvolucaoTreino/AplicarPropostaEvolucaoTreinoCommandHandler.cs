using System.Text.Json;
using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Treinos;
using EquilibraFitPlusPlus.Application.Features.Treinos.Mappings;
using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.AplicarPropostaEvolucaoTreino;

/// <summary>
/// Handles workout evolution proposal application.
/// </summary>
public sealed class AplicarPropostaEvolucaoTreinoCommandHandler : IRequestHandler<AplicarPropostaEvolucaoTreinoCommand, Result<TreinoUsuarioResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ITreinoRepository _treinoRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public AplicarPropostaEvolucaoTreinoCommandHandler(ITreinoRepository treinoRepository, IUnitOfWork unitOfWork)
    {
        _treinoRepository = treinoRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<TreinoUsuarioResponse>> Handle(AplicarPropostaEvolucaoTreinoCommand command, CancellationToken cancellationToken)
    {
        TreinoEvolucaoProposta? proposal = await _treinoRepository.ObterPropostaEvolucaoAsync(
            command.TenantId,
            command.UsuarioId,
            command.PropostaId,
            cancellationToken);

        if (proposal?.TreinoUsuario is null || proposal.TreinoUsuarioId != command.TreinoId)
        {
            return Result<TreinoUsuarioResponse>.Failure(new Error("treinos.proposta_nao_encontrada", "Proposta não encontrada. Podemos gerar uma nova quando você quiser."));
        }

        if (!proposal.Status.Equals("Pendente", StringComparison.OrdinalIgnoreCase))
        {
            return Result<TreinoUsuarioResponse>.Failure(new Error("treinos.proposta_ja_decidida", "Essa proposta já foi decidida."));
        }

        CriarTreinoRequest? plan = JsonSerializer.Deserialize<CriarTreinoRequest>(proposal.PlanoJson, JsonOptions);
        if (plan is null || plan.Exercicios.Count == 0)
        {
            return Result<TreinoUsuarioResponse>.Failure(new Error("treinos.proposta_invalida", "Não conseguimos ler essa proposta. Podemos gerar uma nova."));
        }

        Guid[] exerciseIds = plan.Exercicios
            .Where(item => item.ExercicioId.HasValue)
            .Select(item => item.ExercicioId!.Value)
            .Distinct()
            .ToArray();

        IReadOnlyDictionary<Guid, Exercicio> existingExercises = await _treinoRepository.ObterExerciciosPorIdsAsync(command.TenantId, exerciseIds, cancellationToken);
        if (existingExercises.Count != exerciseIds.Length)
        {
            return Result<TreinoUsuarioResponse>.Failure(new Error("treinos.exercicio_nao_encontrado", "Um exercício da proposta não foi encontrado. Podemos gerar uma nova proposta."));
        }

        TreinoUsuario previous = proposal.TreinoUsuario;
        previous.Ativo = false;
        previous.DataFim = DateOnly.FromDateTime(DateTime.UtcNow);
        previous.MotivoFinalizacao = "Evolução de treino confirmada pelo usuário.";

        var nextPlan = new TreinoUsuario
        {
            TenantId = command.TenantId,
            UsuarioId = command.UsuarioId,
            Nome = plan.Nome.Trim(),
            Objetivo = plan.Objetivo.Trim(),
            FrequenciaSemanal = plan.FrequenciaSemanal,
            Versao = proposal.VersaoProposta,
            TreinoAnteriorId = previous.Id,
            DataInicio = plan.DataInicio ?? DateOnly.FromDateTime(DateTime.UtcNow),
            DuracaoSemanas = plan.DuracaoSemanas ?? previous.DuracaoSemanas,
            Fase = string.IsNullOrWhiteSpace(plan.Fase) ? $"Fase {proposal.VersaoProposta}" : plan.Fase.Trim(),
            Ativo = true
        };

        foreach (TreinoExercicioRequest item in plan.Exercicios.OrderBy(item => item.DiaTreino ?? 1).ThenBy(item => item.Ordem))
        {
            Exercicio exercise = item.ExercicioId.HasValue
                ? existingExercises[item.ExercicioId.Value]
                : CreateExercise(command.TenantId, item);

            if (!item.ExercicioId.HasValue)
            {
                _treinoRepository.AdicionarExercicio(exercise);
            }

            nextPlan.Exercicios.Add(new TreinoExercicio
            {
                TenantId = command.TenantId,
                Exercicio = exercise,
                ExercicioId = exercise.Id,
                DiaTreino = item.DiaTreino ?? 1,
                Ordem = item.Ordem,
                Series = item.Series,
                Repeticoes = item.Repeticoes.Trim(),
                DescansoSegundos = item.DescansoSegundos,
                CargaAlvoKg = item.CargaAlvoKg,
                RpeAlvo = item.RpeAlvo,
                RepeticoesMin = item.RepeticoesMin,
                RepeticoesMax = item.RepeticoesMax,
                ProgressaoMotivo = string.IsNullOrWhiteSpace(item.ProgressaoMotivo) ? null : item.ProgressaoMotivo.Trim(),
                Observacao = string.IsNullOrWhiteSpace(item.Observacao) ? null : item.Observacao.Trim()
            });
        }

        proposal.Status = "Aplicada";
        proposal.AplicadaEm = DateTimeOffset.UtcNow;

        _treinoRepository.AdicionarTreino(nextPlan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TreinoUsuarioResponse>.Success(TreinoMapper.Map(nextPlan));
    }

    private static Exercicio CreateExercise(Guid tenantId, TreinoExercicioRequest request)
    {
        return new Exercicio
        {
            TenantId = tenantId,
            Nome = request.Nome!.Trim(),
            GrupoMuscular = request.GrupoMuscular!.Trim(),
            Nivel = string.IsNullOrWhiteSpace(request.Nivel) ? "Iniciante" : request.Nivel.Trim(),
            Equipamento = string.IsNullOrWhiteSpace(request.Equipamento) ? null : request.Equipamento.Trim(),
            Instrucao = request.Instrucao!.Trim()
        };
    }
}
