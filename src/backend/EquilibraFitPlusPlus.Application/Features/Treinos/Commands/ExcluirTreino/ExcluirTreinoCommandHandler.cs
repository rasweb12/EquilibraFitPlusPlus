using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Treinos;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.ExcluirTreino;

/// <summary>
/// Handles workout soft deletion.
/// </summary>
public sealed class ExcluirTreinoCommandHandler : IRequestHandler<ExcluirTreinoCommand, Result>
{
    private readonly ITreinoRepository _treinoRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public ExcluirTreinoCommandHandler(ITreinoRepository treinoRepository, IUnitOfWork unitOfWork)
    {
        _treinoRepository = treinoRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ExcluirTreinoCommand request, CancellationToken cancellationToken)
    {
        TreinoUsuario? treino = await _treinoRepository.ObterTreinoPorIdAsync(request.TenantId, request.UsuarioId, request.TreinoId, cancellationToken);
        if (treino is null)
        {
            return Result.Failure(new Error("treinos.nao_encontrado", "Não encontramos esse treino."));
        }

        treino.Ativo = false;
        treino.ExcluidoEm = DateTimeOffset.UtcNow;
        treino.ExcluidoPor = request.UsuarioId;
        treino.MotivoExclusao = "Exclusão solicitada pelo usuário.";

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
