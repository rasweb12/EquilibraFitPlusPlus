using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Treinos;
using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.ConcluirTreinoExercicio;

/// <summary>
/// Handles workout exercise completion updates.
/// </summary>
public sealed class ConcluirTreinoExercicioCommandHandler : IRequestHandler<ConcluirTreinoExercicioCommand, Result<TreinoExercicioConclusaoResponse>>
{
    private readonly ITreinoRepository _treinoRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public ConcluirTreinoExercicioCommandHandler(ITreinoRepository treinoRepository, IUnitOfWork unitOfWork)
    {
        _treinoRepository = treinoRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<TreinoExercicioConclusaoResponse>> Handle(ConcluirTreinoExercicioCommand command, CancellationToken cancellationToken)
    {
        TreinoUsuario? treino = await _treinoRepository.ObterTreinoPorIdAsync(
            command.TenantId,
            command.UsuarioId,
            command.TreinoId,
            cancellationToken);

        if (treino is null)
        {
            return Result<TreinoExercicioConclusaoResponse>.Failure(new Error("treinos.nao_encontrado", "Treino não encontrado. Podemos atualizar a tela e tentar novamente."));
        }

        if (treino.Exercicios.All(exercicio => exercicio.Id != command.TreinoExercicioId))
        {
            return Result<TreinoExercicioConclusaoResponse>.Failure(new Error("treinos.exercicio_nao_encontrado", "Exercício não encontrado neste treino. Podemos atualizar a tela e tentar novamente."));
        }

        DateOnly data = command.Request.Data ?? DateOnly.FromDateTime(DateTime.UtcNow);
        TreinoExercicioConclusao? conclusao = await _treinoRepository.ObterConclusaoAsync(
            command.TenantId,
            command.UsuarioId,
            command.TreinoExercicioId,
            data,
            cancellationToken);

        DateTimeOffset? concluidoEm = command.Request.Concluido ? DateTimeOffset.UtcNow : null;
        if (conclusao is null)
        {
            conclusao = new TreinoExercicioConclusao
            {
                TenantId = command.TenantId,
                UsuarioId = command.UsuarioId,
                TreinoUsuarioId = command.TreinoId,
                TreinoExercicioId = command.TreinoExercicioId,
                Data = data
            };

            _treinoRepository.AdicionarConclusao(conclusao);
        }

        conclusao.Concluido = command.Request.Concluido;
        conclusao.ConcluidoEm = concluidoEm;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TreinoExercicioConclusaoResponse>.Success(new TreinoExercicioConclusaoResponse(
            conclusao.TreinoUsuarioId,
            conclusao.TreinoExercicioId,
            conclusao.Data,
            conclusao.Concluido,
            conclusao.ConcluidoEm,
            conclusao.Concluido
                ? "Exercicio marcado. O importante e manter constancia no seu ritmo."
                : "Sem problemas. Podemos ajustar o treino de hoje com calma."));
    }
}
