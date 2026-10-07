using EquilibraFitPlusPlus.Contracts.Admin;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Queries.ObterAdminIaOperacao;

/// <summary>
/// Query used to get the AI operation overview.
/// </summary>
public sealed record ObterAdminIaOperacaoQuery(Guid TenantId, DateOnly DataReferencia) : IRequest<Result<AdminIaOperacaoResponse>>;
