using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Queries.ObterAdminDashboard;

/// <summary>
/// Validates administrative dashboard reads.
/// </summary>
public sealed class ObterAdminDashboardQueryValidator : AbstractValidator<ObterAdminDashboardQuery>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public ObterAdminDashboardQueryValidator()
    {
        RuleFor(query => query.TenantId).NotEmpty();
    }
}
