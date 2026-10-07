using EquilibraFitPlusPlus.Application.Abstractions.Treinos;
using EquilibraFitPlusPlus.Application.Features.Treinos.Mappings;
using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Pagination;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Queries.ListarExercicios;

/// <summary>
/// Handles exercise catalog listing.
/// </summary>
public sealed class ListarExerciciosQueryHandler : IRequestHandler<ListarExerciciosQuery, Result<PagedResult<ExercicioResponse>>>
{
    private readonly ITreinoRepository _treinoRepository;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public ListarExerciciosQueryHandler(ITreinoRepository treinoRepository)
    {
        _treinoRepository = treinoRepository;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<ExercicioResponse>>> Handle(ListarExerciciosQuery request, CancellationToken cancellationToken)
    {
        PagedResult<Exercicio> exercicios = await _treinoRepository.ListarExerciciosAsync(
            request.TenantId,
            request.Termo,
            request.Page,
            request.PageSize,
            cancellationToken);

        var response = new PagedResult<ExercicioResponse>(
            exercicios.Items.Select(TreinoMapper.Map).ToArray(),
            exercicios.Page,
            exercicios.PageSize,
            exercicios.TotalItems);

        return Result<PagedResult<ExercicioResponse>>.Success(response);
    }
}
