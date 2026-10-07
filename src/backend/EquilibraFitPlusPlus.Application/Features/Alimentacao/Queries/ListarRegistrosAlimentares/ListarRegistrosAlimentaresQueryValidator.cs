using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Alimentacao.Queries.ListarRegistrosAlimentares;

/// <summary>
/// Validates food log listing.
/// </summary>
public sealed class ListarRegistrosAlimentaresQueryValidator : AbstractValidator<ListarRegistrosAlimentaresQuery>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public ListarRegistrosAlimentaresQueryValidator()
    {
        RuleFor(query => query.TenantId).NotEmpty();
        RuleFor(query => query.UsuarioId).NotEmpty();
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query)
            .Must(query => query.Inicio is null || query.Fim is null || query.Inicio <= query.Fim)
            .WithMessage("O período informado para alimentação é inválido.");
    }
}
