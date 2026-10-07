using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Premium.Commands.SalvarCupom;

/// <summary>
/// Validates coupon commands.
/// </summary>
public sealed class SalvarCupomCommandValidator : AbstractValidator<SalvarCupomCommand>
{
    /// <summary>Initializes validation rules.</summary>
    public SalvarCupomCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.UsuarioId).NotEmpty();
        RuleFor(command => command.Request.Codigo).NotEmpty().MaximumLength(80);
        RuleFor(command => command.Request.PercentualDesconto).InclusiveBetween(0, 100).When(command => command.Request.PercentualDesconto.HasValue);
        RuleFor(command => command.Request.ValorDesconto).GreaterThan(0).When(command => command.Request.ValorDesconto.HasValue);
        RuleFor(command => command.Request.UsoMaximo).GreaterThan(0).When(command => command.Request.UsoMaximo.HasValue);
        RuleFor(command => command.Request)
            .Must(request => request.PercentualDesconto.HasValue ^ request.ValorDesconto.HasValue)
            .WithMessage("Informe percentual ou valor fixo, apenas um deles.");
    }
}
