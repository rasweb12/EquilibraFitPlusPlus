using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.AiCoach.Queries.ListarCoachSessions;

/// <summary>
/// Validates AI Coach session listing.
/// </summary>
public sealed class ListarCoachSessionsQueryValidator : AbstractValidator<ListarCoachSessionsQuery>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public ListarCoachSessionsQueryValidator()
    {
        RuleFor(query => query.TenantId).NotEmpty();
        RuleFor(query => query.UsuarioId).NotEmpty();
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
