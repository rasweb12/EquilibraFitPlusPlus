using EquilibraFitPlusPlus.Domain.Enums;
using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Alimentacao.Commands.AtualizarRegistroAlimentar;

/// <summary>
/// Validates food log updates.
/// </summary>
public sealed class AtualizarRegistroAlimentarCommandValidator : AbstractValidator<AtualizarRegistroAlimentarCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public AtualizarRegistroAlimentarCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.UsuarioId).NotEmpty();
        RuleFor(command => command.RegistroId).NotEmpty();
        RuleFor(command => command.Request.DataHora)
            .LessThanOrEqualTo(DateTimeOffset.UtcNow.AddDays(1))
            .WithMessage("A data da refeição não pode ficar muito à frente de hoje.");
        RuleFor(command => command.Request.TipoRefeicao)
            .NotEmpty()
            .Must(value => Enum.TryParse<TipoRefeicao>(value, true, out _))
            .WithMessage("Informe um tipo de refeição válido.");
        RuleFor(command => command.Request.Itens).NotEmpty().Must(items => items.Count <= 30).WithMessage("Uma refeição pode conter até 30 itens.");
        RuleForEach(command => command.Request.Itens).ChildRules(item =>
        {
            item.RuleFor(x => x.Nome).NotEmpty().MaximumLength(160);
            item.RuleFor(x => x.Quantidade).GreaterThan(0).LessThanOrEqualTo(10000);
            item.RuleFor(x => x.Unidade).NotEmpty().MaximumLength(40);
            item.RuleFor(x => x.Calorias).GreaterThanOrEqualTo(0).LessThanOrEqualTo(10000);
            item.RuleFor(x => x.ProteinaG).GreaterThanOrEqualTo(0).LessThanOrEqualTo(1000);
            item.RuleFor(x => x.CarboidratoG).GreaterThanOrEqualTo(0).LessThanOrEqualTo(1000);
            item.RuleFor(x => x.GorduraG).GreaterThanOrEqualTo(0).LessThanOrEqualTo(1000);
            item.RuleFor(x => x.FonteNutricional).MaximumLength(40);
        });
    }
}
