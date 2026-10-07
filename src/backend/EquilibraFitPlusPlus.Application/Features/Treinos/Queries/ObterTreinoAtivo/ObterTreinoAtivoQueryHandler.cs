using EquilibraFitPlusPlus.Application.Abstractions.Treinos;
using EquilibraFitPlusPlus.Application.Features.Treinos.Mappings;
using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Queries.ObterTreinoAtivo;

/// <summary>
/// Handles active workout reads.
/// </summary>
public sealed class ObterTreinoAtivoQueryHandler : IRequestHandler<ObterTreinoAtivoQuery, Result<TreinoUsuarioResponse>>
{
    private readonly ITreinoRepository _treinoRepository;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public ObterTreinoAtivoQueryHandler(ITreinoRepository treinoRepository)
    {
        _treinoRepository = treinoRepository;
    }

    /// <inheritdoc />
    public async Task<Result<TreinoUsuarioResponse>> Handle(ObterTreinoAtivoQuery request, CancellationToken cancellationToken)
    {
        TreinoUsuario? treino = await _treinoRepository.ObterTreinoAtivoAsync(request.TenantId, request.UsuarioId, cancellationToken);
        if (treino is null)
        {
            return Result<TreinoUsuarioResponse>.Failure(new Error("treinos.ativo_nao_encontrado", "Nenhum treino ativo por enquanto. Podemos montar um quando você estiver pronto."));
        }

        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        IReadOnlyCollection<TreinoExercicioConclusao> conclusoes = await _treinoRepository.ListarConclusoesAsync(
            request.TenantId,
            request.UsuarioId,
            treino.Id,
            today,
            cancellationToken);

        return Result<TreinoUsuarioResponse>.Success(TreinoMapper.Map(treino, conclusoes, today));
    }
}
