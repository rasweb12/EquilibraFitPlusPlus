using EquilibraFitPlusPlus.Contracts.Lgpd;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Lgpd.Queries.ObterLgpdAdmin;

/// <summary>
/// Query used to get administrative LGPD summary.
/// </summary>
public sealed record ObterLgpdAdminQuery(Guid TenantId) : IRequest<Result<LgpdAdminResumoResponse>>;
