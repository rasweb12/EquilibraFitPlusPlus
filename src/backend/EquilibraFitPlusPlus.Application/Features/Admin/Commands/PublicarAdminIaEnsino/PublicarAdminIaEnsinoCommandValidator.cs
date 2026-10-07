using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Commands.PublicarAdminIaEnsino;

/// <summary>
/// Validates AI teaching publication.
/// </summary>
public sealed class PublicarAdminIaEnsinoCommandValidator : AbstractValidator<PublicarAdminIaEnsinoCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public PublicarAdminIaEnsinoCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.AdminUsuarioId).NotEmpty();
        RuleFor(command => command.Request.InstrucoesCoach).NotEmpty().MaximumLength(4000);
        RuleFor(command => command.Request.RegrasAlimentacao).NotEmpty().MaximumLength(4000);
        RuleFor(command => command.Request.RegrasTreino).NotEmpty().MaximumLength(4000);
        RuleFor(command => command.Request.BaseConhecimento).NotEmpty().MaximumLength(4000);
        RuleFor(command => command.Request.ExemplosBoasRespostas).NotEmpty().MaximumLength(4000);
        RuleFor(command => command.Request.Observacao).MaximumLength(500);
    }
}
