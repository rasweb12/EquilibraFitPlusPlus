using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Habitos.Commands.SalvarHabitosDiarios;

/// <summary>
/// Validates daily habits persistence.
/// </summary>
public sealed class SalvarHabitosDiariosCommandValidator : AbstractValidator<SalvarHabitosDiariosCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public SalvarHabitosDiariosCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.UsuarioId).NotEmpty();
        RuleFor(command => command.Request.AguaMl).InclusiveBetween(0, 10000).WithMessage("Informe a água consumida em um intervalo seguro.");
        RuleFor(command => command.Request.MetaAguaMl).InclusiveBetween(250, 10000).WithMessage("Informe uma meta de água segura.");
        RuleFor(command => command.Request.SonoHoras).InclusiveBetween(0m, 24m).WithMessage("Informe uma duração de sono válida.");
        RuleFor(command => command.Request.MetaSonoHoras).InclusiveBetween(1m, 14m).WithMessage("Informe uma meta de sono segura.");
        RuleFor(command => command.Request.Humor).InclusiveBetween(1, 5).WithMessage("Informe o humor entre 1 e 5.");
        RuleFor(command => command.Request.Data)
            .Must(data => !data.HasValue || data.Value <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage("A data do registro deve estar dentro de um período seguro.");
    }
}
