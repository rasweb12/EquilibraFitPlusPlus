using System;
using System.Collections.Generic;
using System.Text;
using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Treinos;
using EquilibraFitPlusPlus.Application.Features.Treinos.Mappings;
using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.RemoverTreinoExercicio;

public sealed class RemoverTreinoExercicioCommandHandler
    : IRequestHandler<
        RemoverTreinoExercicioCommand,
        Result<TreinoUsuarioResponse>>
{
    private readonly ITreinoRepository _treinoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoverTreinoExercicioCommandHandler(
        ITreinoRepository treinoRepository,
        IUnitOfWork unitOfWork)
    {
        _treinoRepository = treinoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TreinoUsuarioResponse>> Handle(
        RemoverTreinoExercicioCommand command,
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
                    "Exercício não encontrado neste treino."));
        }

        if (!exercise.TreinoUsuario.Ativo)
        {
            return Result<TreinoUsuarioResponse>.Failure(
                new Error(
                    "treinos.inativo",
                    "Não é possível alterar um plano histórico."));
        }

        exercise.ExcluidoEm =
            DateTimeOffset.UtcNow;

        exercise.ExcluidoPor =
            command.UsuarioId;

        exercise.MotivoExclusao =
            "Removido manualmente do plano de treino.";

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        TreinoUsuario treino =
            await _treinoRepository.ObterTreinoPorIdAsync(
                command.TenantId,
                command.UsuarioId,
                command.TreinoId,
                cancellationToken)
            ?? exercise.TreinoUsuario;

        return Result<TreinoUsuarioResponse>.Success(
            TreinoMapper.Map(treino));
    }
}
