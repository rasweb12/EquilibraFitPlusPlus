using EquilibraFitPlusPlus.Contracts.Admin;
using EquilibraFitPlusPlus.Shared.Pagination;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Queries.ListarFeatureFlags;

/// <summary>
/// Query used to list feature flags.
/// </summary>
public sealed record ListarFeatureFlagsQuery(Guid TenantId, int Page, int PageSize)
    : IRequest<Result<PagedResult<FeatureFlagResponse>>>;
