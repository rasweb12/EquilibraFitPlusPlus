using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Dashboard.Queries.ObterDashboardUsuario;

/// <summary>
/// Validates user dashboard queries.
/// </summary>
public sealed class ObterDashboardUsuarioQueryValidator : AbstractValidator<ObterDashboardUsuarioQuery>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public ObterDashboardUsuarioQueryValidator()
    {
        RuleFor(query => query.TenantId).NotEmpty();
        RuleFor(query => query.UsuarioId).NotEmpty();
        RuleFor(query => query.Data)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage("A data do dashboard não pode ficar muito à frente de hoje.");
    }
}
