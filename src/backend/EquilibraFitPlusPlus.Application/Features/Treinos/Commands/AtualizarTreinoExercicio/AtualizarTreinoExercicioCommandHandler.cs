using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Treinos;
using EquilibraFitPlusPlus.Application.Features.Treinos.Mappings;
using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.AtualizarTreinoExercicio;

/// <summary>
/// Handles prescribed exercise updates.
/// </summary>
public sealed class AtualizarTreinoExercicioCommandHandler
    : IRequestHandler<
        AtualizarTreinoExercicioCommand,
        Result<TreinoUsuarioResponse>>
{
    private readonly ITreinoRepository _treinoRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public AtualizarTreinoExercicioCommandHandler(
        ITreinoRepository treinoRepository,
        IUnitOfWork unitOfWork)
    {
        _treinoRepository = treinoRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<TreinoUsuarioResponse>> Handle(
        AtualizarTreinoExercicioCommand command,
        CancellationToken cancellationToken)
    {
        TreinoExercicio? exercise =
            await _treinoRepository.ObterTreinoExercicioAsync(
                command.TenantId,
                command.UsuarioId,
                command.TreinoId,
                command.TreinoExercicioId,
                cancellationToken);

        if (exercise?.TreinoUsuario is null)
        {
            return Result<TreinoUsuarioResponse>.Failure(
                new Error(
                    "treinos.exercicio_nao_encontrado",
                    "Exercício não encontrado neste treino. "
                    + "Podemos atualizar a tela e tentar novamente."));
        }

        TreinoUsuario treino = exercise.TreinoUsuario;

        if (!treino.Ativo)
        {
            return Result<TreinoUsuarioResponse>.Failure(
                new Error(
                    "treinos.inativo",
                    "Não é possível alterar um plano histórico."));
        }

        if (command.Request.DiaTreino < 1 ||
            command.Request.DiaTreino > treino.FrequenciaSemanal)
        {
            return Result<TreinoUsuarioResponse>.Failure(
                new Error(
                    "treinos.dia_invalido",
                    "O dia informado não pertence à frequência atual do treino."));
        }

        byte currentDay = exercise.DiaTreino;
        int currentOrder = exercise.Ordem;

        byte targetDay = command.Request.DiaTreino;
        int requestedOrder = command.Request.Ordem;

        bool changingDay = currentDay != targetDay;

        List<TreinoExercicio> targetDayExercises = treino.Exercicios
            .Where(item =>
                item.Id != exercise.Id &&
                item.ExcluidoEm == null &&
                item.DiaTreino == targetDay)
            .OrderBy(item => item.Ordem)
            .ToList();

        int nextOrder = targetDayExercises.Count == 0
            ? 1
            : targetDayExercises.Max(item => item.Ordem) + 1;

        bool requestedOrderOccupied = targetDayExercises.Any(
            item => item.Ordem == requestedOrder);

        int finalOrder;

        if (changingDay)
        {
            /*
             * Ao mover para outro dia:
             *
             * - se a ordem solicitada for válida e estiver livre, mantém;
             * - se estiver ocupada, adiciona automaticamente ao fim;
             * - se vier 0 ou negativo, também adiciona ao fim.
             *
             * Isso evita o erro:
             * "Já existe um exercício nesta posição do dia."
             */
            finalOrder = targetDayExercises.Count == 0
                ? 1
                : requestedOrder <= 0 || requestedOrderOccupied
                    ? nextOrder
                    : requestedOrder;
        }
        else
        {
            /*
             * Dentro do mesmo dia ainda protegemos posições duplicadas.
             * Reordenação completa pode ser implementada separadamente.
             */
            if (requestedOrder <= 0)
            {
                finalOrder = currentOrder;
            }
            else if (requestedOrderOccupied &&
                     requestedOrder != currentOrder)
            {
                return Result<TreinoUsuarioResponse>.Failure(
                    new Error(
                        "treinos.ordem_duplicada",
                        "Já existe um exercício nesta posição do dia."));
            }
            else
            {
                finalOrder = requestedOrder;
            }
        }

        exercise.DiaTreino = targetDay;
        exercise.Ordem = finalOrder;

        exercise.Series = command.Request.Series;

        exercise.Repeticoes =
            command.Request.Repeticoes.Trim();

        exercise.DescansoSegundos =
            command.Request.DescansoSegundos;

        exercise.CargaAlvoKg =
            command.Request.CargaAlvoKg;

        exercise.RpeAlvo =
            command.Request.RpeAlvo;

        exercise.RepeticoesMin =
            command.Request.RepeticoesMin;

        exercise.RepeticoesMax =
            command.Request.RepeticoesMax;

        exercise.ProgressaoMotivo =
            string.IsNullOrWhiteSpace(
                command.Request.ProgressaoMotivo)
                ? null
                : command.Request.ProgressaoMotivo.Trim();

        exercise.Observacao =
            string.IsNullOrWhiteSpace(
                command.Request.Observacao)
                ? null
                : command.Request.Observacao.Trim();

        if (exercise.Exercicio is not null)
        {
            exercise.Exercicio.Equipamento =
                string.IsNullOrWhiteSpace(
                    command.Request.Equipamento)
                    ? null
                    : command.Request.Equipamento.Trim();
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }
        catch (PersistenceConflictException)
        {
            return Result<TreinoUsuarioResponse>.Failure(
                new Error(
                    "treinos.posicao_concorrente",
                    "O treino foi alterado enquanto você fazia essa mudança. "
                    + "Atualize a tela e tente novamente."));
        }

        TreinoUsuario atualizado =
            await _treinoRepository.ObterTreinoPorIdAsync(
                command.TenantId,
                command.UsuarioId,
                command.TreinoId,
                cancellationToken)
            ?? treino;

        return Result<TreinoUsuarioResponse>.Success(
            TreinoMapper.Map(atualizado));
    }
}
