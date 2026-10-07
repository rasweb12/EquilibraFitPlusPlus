using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Queries.ListarTreinos;

/// <summary>
/// Validates workout listing.
/// </summary>
public sealed class ListarTreinosQueryValidator : AbstractValidator<ListarTreinosQuery>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public ListarTreinosQueryValidator()
    {
        RuleFor(query => query.TenantId).NotEmpty();
        RuleFor(query => query.UsuarioId).NotEmpty();
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
