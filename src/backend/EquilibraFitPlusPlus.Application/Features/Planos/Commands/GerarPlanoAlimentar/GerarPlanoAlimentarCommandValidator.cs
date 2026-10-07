using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Planos.Commands.GerarPlanoAlimentar;

/// <summary>
/// Validates generated diet plan requests.
/// </summary>
public sealed class GerarPlanoAlimentarCommandValidator : AbstractValidator<GerarPlanoAlimentarCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public GerarPlanoAlimentarCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.UsuarioId).NotEmpty();
        RuleFor(command => command.Request.Rotina).MaximumLength(500);
        RuleFor(command => command.Request.Preferencias).Must(items => items is null || items.Count <= 30).WithMessage("Informe até 30 preferências.");
        RuleFor(command => command.Request.Restricoes).Must(items => items is null || items.Count <= 30).WithMessage("Informe até 30 restrições.");
        RuleForEach(command => command.Request.Preferencias).MaximumLength(120);
        RuleForEach(command => command.Request.Restricoes).MaximumLength(120);
    }
}
