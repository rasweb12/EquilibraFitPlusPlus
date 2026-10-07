using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.GerarTreinoIa;

/// <summary>
/// Validates AI workout generation requests.
/// </summary>
public sealed class GerarTreinoIaCommandValidator : AbstractValidator<GerarTreinoIaCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public GerarTreinoIaCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.UsuarioId).NotEmpty();
        RuleFor(command => command.Request.Objetivo).MaximumLength(160);
        RuleFor(command => command.Request.Nivel).MaximumLength(40);
        RuleFor(command => command.Request.DiasPorSemana)
            .Must(value => !value.HasValue || value.Value is >= 1 and <= 7)
            .WithMessage("Informe entre 1 e 7 dias por semana.");
        RuleFor(command => command.Request.DuracaoMinutos)
            .Must(value => !value.HasValue || value.Value is >= 10 and <= 240)
            .WithMessage("Informe uma duração entre 10 e 240 minutos.");
        RuleFor(command => command.Request.DuracaoSemanas)
            .Must(value => !value.HasValue || value.Value is >= 1 and <= 52)
            .WithMessage("Informe uma duração entre 1 e 52 semanas.");
        RuleFor(command => command.Request.Limitacoes).Must(items => items is null || items.Count <= 20).WithMessage("Informe até 20 limitações.");
        RuleFor(command => command.Request.Equipamentos).Must(items => items is null || items.Count <= 20).WithMessage("Informe até 20 equipamentos.");
        RuleFor(command => command.Request.GruposMuscularesPrioritarios).Must(items => items is null || items.Count <= 20).WithMessage("Informe até 20 grupos prioritários.");
        RuleForEach(command => command.Request.Limitacoes).MaximumLength(120);
        RuleForEach(command => command.Request.Equipamentos).MaximumLength(120);
        RuleForEach(command => command.Request.GruposMuscularesPrioritarios).MaximumLength(120);
    }
}
