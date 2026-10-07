using EquilibraFitPlusPlus.Domain.Enums;
using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Onboarding.Commands.SalvarQuestionario;

/// <summary>
/// Validates questionnaire commands.
/// </summary>
public sealed class SalvarQuestionarioCommandValidator : AbstractValidator<SalvarQuestionarioCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public SalvarQuestionarioCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.UsuarioId).NotEmpty();
        RuleFor(command => command.Request.DataNascimento)
            .Must(date => date <= DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-13)))
            .WithMessage("Para usar o EquilibraFit++, a idade minima informada deve ser de 13 anos.");
        RuleFor(command => command.Request.SexoBiologico)
            .NotEmpty()
            .Must(value => Enum.TryParse<SexoBiologico>(value, true, out _))
            .WithMessage("Informe uma opcao valida para sexo biologico.");
        RuleFor(command => command.Request.AlturaCm).InclusiveBetween(80, 250);
        RuleFor(command => command.Request.PesoAtualKg).InclusiveBetween(25, 350);
        RuleFor(command => command.Request.Objetivo)
            .NotEmpty()
            .Must(value => Enum.TryParse<ObjetivoSaude>(value, true, out _))
            .WithMessage("Informe um objetivo válido.");
        RuleFor(command => command.Request.NivelAtividade)
            .NotEmpty()
            .Must(value => Enum.TryParse<NivelAtividade>(value, true, out _))
            .WithMessage("Informe um nível de atividade válido.");
        RuleFor(command => command.Request.DiasTreinoSemana).InclusiveBetween((byte)0, (byte)7);
        RuleForEach(command => command.Request.Preferencias).MaximumLength(120);
        RuleForEach(command => command.Request.Restricoes).MaximumLength(120);
        RuleForEach(command => command.Request.Observacoes).MaximumLength(240);
    }
}
