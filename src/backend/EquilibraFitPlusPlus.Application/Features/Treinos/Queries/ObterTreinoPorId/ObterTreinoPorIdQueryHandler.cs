using EquilibraFitPlusPlus.Application.Abstractions.Treinos;
using EquilibraFitPlusPlus.Application.Features.Treinos.Mappings;
using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Queries.ObterTreinoPorId;

/// <summary>
/// Handles workout reads by identifier.
/// </summary>
public sealed class ObterTreinoPorIdQueryHandler : IRequestHandler<ObterTreinoPorIdQuery, Result<TreinoUsuarioResponse>>
{
    private readonly ITreinoRepository _treinoRepository;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public ObterTreinoPorIdQueryHandler(ITreinoRepository treinoRepository)
    {
        _treinoRepository = treinoRepository;
    }

    /// <inheritdoc />
    public async Task<Result<TreinoUsuarioResponse>> Handle(ObterTreinoPorIdQuery request, CancellationToken cancellationToken)
    {
        TreinoUsuario? treino = await _treinoRepository.ObterTreinoPorIdAsync(request.TenantId, request.UsuarioId, request.TreinoId, cancellationToken);
        if (treino is null)
        {
            return Result<TreinoUsuarioResponse>.Failure(new Error("treinos.nao_encontrado", "Treino não encontrado. Podemos atualizar a lista e tentar novamente."));
        }

        return Result<TreinoUsuarioResponse>.Success(TreinoMapper.Map(treino));
    }
}
