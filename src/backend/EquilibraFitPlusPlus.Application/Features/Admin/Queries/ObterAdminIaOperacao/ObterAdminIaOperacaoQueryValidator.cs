using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Queries.ObterAdminIaOperacao;

/// <summary>
/// Validates AI operation overview reads.
/// </summary>
public sealed class ObterAdminIaOperacaoQueryValidator : AbstractValidator<ObterAdminIaOperacaoQuery>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public ObterAdminIaOperacaoQueryValidator()
    {
        RuleFor(query => query.TenantId).NotEmpty();
    }
}
