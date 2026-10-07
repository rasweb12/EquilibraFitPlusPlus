using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Commands.TestarAdminIaEnsino;

/// <summary>
/// Validates AI teaching tests.
/// </summary>
public sealed class TestarAdminIaEnsinoCommandValidator : AbstractValidator<TestarAdminIaEnsinoCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public TestarAdminIaEnsinoCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.AdminUsuarioId).NotEmpty();
        RuleFor(command => command.Request.Pergunta).NotEmpty().MaximumLength(1000);
        RuleFor(command => command.Request.InstrucoesCoach).NotEmpty().MaximumLength(4000);
        RuleFor(command => command.Request.RegrasAlimentacao).NotEmpty().MaximumLength(4000);
        RuleFor(command => command.Request.RegrasTreino).NotEmpty().MaximumLength(4000);
        RuleFor(command => command.Request.BaseConhecimento).NotEmpty().MaximumLength(4000);
        RuleFor(command => command.Request.ExemplosBoasRespostas).NotEmpty().MaximumLength(4000);
    }
}
