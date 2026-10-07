using EquilibraFitPlusPlus.Application.Abstractions.Admin;
using EquilibraFitPlusPlus.Application.Features.Admin.Mappings;
using EquilibraFitPlusPlus.Contracts.Admin;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Pagination;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Queries.ListarFeatureFlags;

/// <summary>
/// Handles feature flag listing.
/// </summary>
public sealed class ListarFeatureFlagsQueryHandler : IRequestHandler<ListarFeatureFlagsQuery, Result<PagedResult<FeatureFlagResponse>>>
{
    private readonly IAdminRepository _adminRepository;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public ListarFeatureFlagsQueryHandler(IAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<FeatureFlagResponse>>> Handle(ListarFeatureFlagsQuery request, CancellationToken cancellationToken)
    {
        PagedResult<FeatureFlag> featureFlags = await _adminRepository.ListarFeatureFlagsAsync(request.TenantId, request.Page, request.PageSize, cancellationToken);
        var response = new PagedResult<FeatureFlagResponse>(
            featureFlags.Items.Select(AdminMapper.Map).ToArray(),
            featureFlags.Page,
            featureFlags.PageSize,
            featureFlags.TotalItems);

        return Result<PagedResult<FeatureFlagResponse>>.Success(response);
    }
}
