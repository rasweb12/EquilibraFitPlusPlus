using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Queries.ListarExercicios;

/// <summary>
/// Validates exercise catalog listing.
/// </summary>
public sealed class ListarExerciciosQueryValidator : AbstractValidator<ListarExerciciosQuery>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public ListarExerciciosQueryValidator()
    {
        RuleFor(query => query.TenantId).NotEmpty();
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Termo).MaximumLength(120);
    }
}
