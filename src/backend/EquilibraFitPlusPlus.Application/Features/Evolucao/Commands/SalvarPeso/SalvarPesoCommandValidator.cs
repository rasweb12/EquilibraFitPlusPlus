using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Evolucao.Commands.SalvarPeso;

/// <summary>
/// Validates body weight progress logs.
/// </summary>
public sealed class SalvarPesoCommandValidator : AbstractValidator<SalvarPesoCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public SalvarPesoCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.UsuarioId).NotEmpty();
        RuleFor(command => command.Request.PesoKg).InclusiveBetween(25m, 350m).WithMessage("Informe um peso válido.");
        RuleFor(command => command.Request.PercentualGordura).InclusiveBetween(0m, 80m).When(command => command.Request.PercentualGordura.HasValue);
        RuleFor(command => command.Request.PercentualMassaMagra).InclusiveBetween(0m, 100m).When(command => command.Request.PercentualMassaMagra.HasValue);
        RuleFor(command => command.Request.Observacao).MaximumLength(500);
        RuleFor(command => command.Request.Data)
            .Must(data => !data.HasValue || data.Value <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage("A data do registro deve estar dentro de um período seguro.");
    }
}
