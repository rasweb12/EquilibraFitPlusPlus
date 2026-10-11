using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Planos.Commands.SubstituirPlanoRefeicao;

/// <summary>
/// Validates a meal-alternative selection.
/// </summary>
public sealed class SubstituirPlanoRefeicaoCommandValidator : AbstractValidator<SubstituirPlanoRefeicaoCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public SubstituirPlanoRefeicaoCommandValidator()
    {
        RuleFor(command => command.PlanoId).NotEmpty();
        RuleFor(command => command.RefeicaoId).NotEmpty();
        RuleFor(command => command.Request.IndiceAlternativa).GreaterThanOrEqualTo(0).LessThan(3);
    }
}

