using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Queries.ListarAuditorias;

/// <summary>
/// Validates administrative audit listing.
/// </summary>
public sealed class ListarAuditoriasQueryValidator : AbstractValidator<ListarAuditoriasQuery>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public ListarAuditoriasQueryValidator()
    {
        RuleFor(query => query.TenantId).NotEmpty();
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Entidade).MaximumLength(120);
    }
}
