using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Treinos;
using EquilibraFitPlusPlus.Application.Features.Treinos.Mappings;
using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.AtualizarTreino;

/// <summary>
/// Handles workout metadata updates.
/// </summary>
public sealed class AtualizarTreinoCommandHandler : IRequestHandler<AtualizarTreinoCommand, Result<TreinoUsuarioResponse>>
{
    private readonly ITreinoRepository _treinoRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public AtualizarTreinoCommandHandler(ITreinoRepository treinoRepository, IUnitOfWork unitOfWork)
    {
        _treinoRepository = treinoRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<TreinoUsuarioResponse>> Handle(AtualizarTreinoCommand command, CancellationToken cancellationToken)
    {
        TreinoUsuario? treino = await _treinoRepository.ObterTreinoPorIdAsync(command.TenantId, command.UsuarioId, command.TreinoId, cancellationToken);
        if (treino is null)
        {
            return Result<TreinoUsuarioResponse>.Failure(new Error("treinos.nao_encontrado", "Treino não encontrado. Podemos atualizar a tela e tentar novamente."));
        }

        treino.Nome = command.Request.Nome.Trim();
        treino.Objetivo = command.Request.Objetivo.Trim();
        treino.FrequenciaSemanal = command.Request.FrequenciaSemanal;
        treino.DataInicio = command.Request.DataInicio;
        treino.DuracaoSemanas = command.Request.DuracaoSemanas;
        treino.Fase = command.Request.Fase.Trim();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<TreinoUsuarioResponse>.Success(TreinoMapper.Map(treino));
    }
}
