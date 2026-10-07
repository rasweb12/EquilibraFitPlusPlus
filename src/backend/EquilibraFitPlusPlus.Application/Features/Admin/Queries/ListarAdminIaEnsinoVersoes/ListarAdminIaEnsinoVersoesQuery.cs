using EquilibraFitPlusPlus.Contracts.Admin;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Queries.ListarAdminIaEnsinoVersoes;

/// <summary>
/// Query used to list AI teaching versions.
/// </summary>
public sealed record ListarAdminIaEnsinoVersoesQuery(Guid TenantId) : IRequest<Result<IReadOnlyCollection<AdminIaEnsinoVersaoResponse>>>;
