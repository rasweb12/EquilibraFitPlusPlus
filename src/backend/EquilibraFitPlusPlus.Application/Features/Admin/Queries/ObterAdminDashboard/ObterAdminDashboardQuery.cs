using EquilibraFitPlusPlus.Contracts.Admin;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Queries.ObterAdminDashboard;

/// <summary>
/// Query used to get the administrative dashboard.
/// </summary>
public sealed record ObterAdminDashboardQuery(Guid TenantId, DateOnly DataReferencia) : IRequest<Result<AdminDashboardResponse>>;
