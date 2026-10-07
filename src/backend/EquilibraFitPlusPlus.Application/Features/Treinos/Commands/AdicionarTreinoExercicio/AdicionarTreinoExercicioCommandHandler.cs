using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Treinos;
using EquilibraFitPlusPlus.Application.Features.Treinos.Mappings;
using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;


namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.AdicionarTreinoExercicio;

public sealed class AdicionarTreinoExercicioCommandHandler
    : IRequestHandler<
        AdicionarTreinoExercicioCommand,
        Result<TreinoUsuarioResponse>>
{
    private readonly ITreinoRepository _treinoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdicionarTreinoExercicioCommandHandler(
        ITreinoRepository treinoRepository,
        IUnitOfWork unitOfWork)
    {
        _treinoRepository = treinoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TreinoUsuarioResponse>> Handle(
        AdicionarTreinoExercicioCommand command,
        CancellationToken cancellationToken)
    {
        TreinoUsuario? treino =
            await _treinoRepository.ObterTreinoPorIdAsync(
                command.TenantId,
                command.UsuarioId,
                command.TreinoId,
                cancellationToken);

        if (treino is null)
        {
            return Result<TreinoUsuarioResponse>.Failure(
                new Error(
                    "treinos.nao_encontrado",
                    "Treino não encontrado."));
        }

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

        List<TreinoExercicio> exercisesOfDay = treino.Exercicios
            .Where(item =>
                item.DiaTreino == command.Request.DiaTreino &&
                item.ExcluidoEm == null)
            .OrderBy(item => item.Ordem)
            .ToList();

        int nextOrder = exercisesOfDay.Count == 0
            ? 1
            : exercisesOfDay.Max(item => item.Ordem) + 1;

        int requestedOrder = command.Request.Ordem;

        bool duplicatedOrder = exercisesOfDay.Any(
            item => item.Ordem == requestedOrder);

        int finalOrder =
            requestedOrder <= 0 || duplicatedOrder
                ? nextOrder
                : requestedOrder;

        Exercicio? exercicio;

        if (command.Request.ExercicioId.HasValue)
        {
            IReadOnlyDictionary<Guid, Exercicio> encontrados =
                await _treinoRepository.ObterExerciciosPorIdsAsync(
                    command.TenantId,
                    new[] { command.Request.ExercicioId.Value },
                    cancellationToken);

            encontrados.TryGetValue(
                command.Request.ExercicioId.Value,
                out exercicio);

            if (exercicio is null)
            {
                return Result<TreinoUsuarioResponse>.Failure(
                    new Error(
                        "treinos.exercicio_catalogo_nao_encontrado",
                        "O exercício selecionado não foi encontrado."));
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(command.Request.Nome))
            {
                return Result<TreinoUsuarioResponse>.Failure(
                    new Error(
                        "treinos.nome_exercicio_obrigatorio",
                        "Informe o nome do exercício."));
            }

            exercicio = new Exercicio
            {
                TenantId = command.TenantId,

                Nome = command.Request.Nome.Trim(),

                GrupoMuscular =
                    string.IsNullOrWhiteSpace(
                        command.Request.GrupoMuscular)
                        ? "Corpo inteiro"
                        : command.Request.GrupoMuscular.Trim(),

                Nivel =
                    string.IsNullOrWhiteSpace(
                        command.Request.Nivel)
                        ? "Iniciante"
                        : command.Request.Nivel.Trim(),

                Equipamento =
                    string.IsNullOrWhiteSpace(
                        command.Request.Equipamento)
                        ? null
                        : command.Request.Equipamento.Trim(),

                Instrucao =
                    string.IsNullOrWhiteSpace(
                        command.Request.Instrucao)
                        ? string.Empty
                        : command.Request.Instrucao.Trim(),
            };

            _treinoRepository.AdicionarExercicio(exercicio);
        }

        var treinoExercicio = new TreinoExercicio
        {
            TenantId = command.TenantId,

            TreinoUsuarioId = treino.Id,

            ExercicioId = exercicio.Id,

            DiaTreino = command.Request.DiaTreino,

            Ordem = finalOrder,

            Series = command.Request.Series,

            Repeticoes =
                command.Request.Repeticoes.Trim(),

            DescansoSegundos =
                command.Request.DescansoSegundos,

            CargaAlvoKg =
                command.Request.CargaAlvoKg,

            RpeAlvo =
                command.Request.RpeAlvo,

            RepeticoesMin =
                command.Request.RepeticoesMin,

            RepeticoesMax =
                command.Request.RepeticoesMax,

            Observacao =
                string.IsNullOrWhiteSpace(
                    command.Request.Observacao)
                    ? null
                    : command.Request.Observacao.Trim(),
        };

        _treinoRepository.AdicionarTreinoExercicio(
            treinoExercicio);

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
                    "Outro exercício ocupou esta posição enquanto o treino "
                    + "era atualizado. Atualize o treino e tente novamente."));
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
