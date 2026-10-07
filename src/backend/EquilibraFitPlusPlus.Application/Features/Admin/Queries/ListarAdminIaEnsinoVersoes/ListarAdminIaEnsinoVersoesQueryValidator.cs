using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Queries.ListarAdminIaEnsinoVersoes;

/// <summary>
/// Validates AI teaching version reads.
/// </summary>
public sealed class ListarAdminIaEnsinoVersoesQueryValidator : AbstractValidator<ListarAdminIaEnsinoVersoesQuery>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public ListarAdminIaEnsinoVersoesQueryValidator()
    {
        RuleFor(query => query.TenantId).NotEmpty();
    }
}
