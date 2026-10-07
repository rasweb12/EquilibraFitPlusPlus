using EquilibraFitPlusPlus.Contracts.Alimentacao;
using EquilibraFitPlusPlus.Shared.Pagination;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Alimentacao.Queries.ListarRegistrosAlimentares;

/// <summary>
/// Query used to list food logs.
/// </summary>
public sealed record ListarRegistrosAlimentaresQuery(Guid TenantId, Guid UsuarioId, DateOnly? Inicio, DateOnly? Fim, int Page, int PageSize)
    : IRequest<Result<PagedResult<RegistroAlimentarResponse>>>;
