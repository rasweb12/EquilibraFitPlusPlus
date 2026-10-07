using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Queries.ListarAdminUsuarios;

/// <summary>
/// Validates administrative user listing.
/// </summary>
public sealed class ListarAdminUsuariosQueryValidator : AbstractValidator<ListarAdminUsuariosQuery>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public ListarAdminUsuariosQueryValidator()
    {
        RuleFor(query => query.TenantId).NotEmpty();
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Termo).MaximumLength(160);
    }
}
