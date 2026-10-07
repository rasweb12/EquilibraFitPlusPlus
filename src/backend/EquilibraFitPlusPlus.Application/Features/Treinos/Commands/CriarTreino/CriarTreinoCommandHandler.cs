using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Treinos;
using EquilibraFitPlusPlus.Application.Abstractions.Users;
using EquilibraFitPlusPlus.Application.Features.Treinos.Mappings;
using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.CriarTreino;

/// <summary>
/// Handles workout creation.
/// </summary>
public sealed class CriarTreinoCommandHandler : IRequestHandler<CriarTreinoCommand, Result<TreinoUsuarioResponse>>
{
    private readonly ITreinoRepository _treinoRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public CriarTreinoCommandHandler(ITreinoRepository treinoRepository, IUsuarioRepository usuarioRepository, IUnitOfWork unitOfWork)
    {
        _treinoRepository = treinoRepository;
        _usuarioRepository = usuarioRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<TreinoUsuarioResponse>> Handle(CriarTreinoCommand command, CancellationToken cancellationToken)
    {
        if (await _usuarioRepository.ObterAtivoAsync(command.TenantId, command.UsuarioId, cancellationToken) is null)
        {
            return Result<TreinoUsuarioResponse>.Failure(new Error("usuario.nao_encontrado", "Não foi possível localizar seu usuário ativo."));
        }

        Guid[] requestedExerciseIds = command.Request.Exercicios
            .Where(item => item.ExercicioId.HasValue)
            .Select(item => item.ExercicioId!.Value)
            .Distinct()
            .ToArray();

        IReadOnlyDictionary<Guid, Exercicio> existingExercises = await _treinoRepository.ObterExerciciosPorIdsAsync(command.TenantId, requestedExerciseIds, cancellationToken);
        if (existingExercises.Count != requestedExerciseIds.Length)
        {
            return Result<TreinoUsuarioResponse>.Failure(new Error("treinos.exercicio_nao_encontrado", "Um dos exercícios informados não foi encontrado."));
        }

        TreinoUsuario? treinoAtivo = await _treinoRepository.ObterTreinoAtivoAsync(command.TenantId, command.UsuarioId, cancellationToken);
        if (treinoAtivo is not null)
        {
            treinoAtivo.Ativo = false;
            treinoAtivo.DataFim = DateOnly.FromDateTime(DateTime.UtcNow);
            treinoAtivo.MotivoFinalizacao = "Novo plano criado pelo usuário.";
        }

        int nextVersion = await _treinoRepository.ObterMaiorVersaoAsync(command.TenantId, command.UsuarioId, cancellationToken) + 1;
        var treino = new TreinoUsuario
        {
            TenantId = command.TenantId,
            UsuarioId = command.UsuarioId,
            Nome = command.Request.Nome.Trim(),
            Objetivo = command.Request.Objetivo.Trim(),
            FrequenciaSemanal = command.Request.FrequenciaSemanal,
            Versao = nextVersion,
            TreinoAnteriorId = treinoAtivo?.Id,
            DataInicio = command.Request.DataInicio ?? DateOnly.FromDateTime(DateTime.UtcNow),
            DuracaoSemanas = command.Request.DuracaoSemanas ?? 6,
            Fase = string.IsNullOrWhiteSpace(command.Request.Fase) ? "Fase 1" : command.Request.Fase.Trim(),
            Ativo = true
        };

        foreach (TreinoExercicioRequest item in command.Request.Exercicios.OrderBy(x => x.DiaTreino ?? 1).ThenBy(x => x.Ordem))
        {
            Exercicio exercicio = item.ExercicioId.HasValue
                ? existingExercises[item.ExercicioId.Value]
                : CreateExercise(command.TenantId, item);

            if (!item.ExercicioId.HasValue)
            {
                _treinoRepository.AdicionarExercicio(exercicio);
            }

            treino.Exercicios.Add(new TreinoExercicio
            {
                TenantId = command.TenantId,
                Exercicio = exercicio,
                ExercicioId = exercicio.Id,
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

        _treinoRepository.AdicionarTreino(treino);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TreinoUsuarioResponse>.Success(TreinoMapper.Map(treino));
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
