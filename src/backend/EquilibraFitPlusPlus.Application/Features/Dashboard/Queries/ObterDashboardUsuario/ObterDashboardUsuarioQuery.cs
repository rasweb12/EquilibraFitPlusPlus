using EquilibraFitPlusPlus.Contracts.Dashboard;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Dashboard.Queries.ObterDashboardUsuario;

/// <summary>
/// Query used to get the current user dashboard.
/// </summary>
public sealed record ObterDashboardUsuarioQuery(Guid TenantId, Guid UsuarioId, DateOnly Data) : IRequest<Result<DashboardUsuarioResponse>>;
