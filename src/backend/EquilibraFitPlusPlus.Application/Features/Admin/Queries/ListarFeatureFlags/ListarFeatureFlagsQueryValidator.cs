using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Queries.ListarFeatureFlags;

/// <summary>
/// Validates feature flag listing.
/// </summary>
public sealed class ListarFeatureFlagsQueryValidator : AbstractValidator<ListarFeatureFlagsQuery>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public ListarFeatureFlagsQueryValidator()
    {
        RuleFor(query => query.TenantId).NotEmpty();
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
