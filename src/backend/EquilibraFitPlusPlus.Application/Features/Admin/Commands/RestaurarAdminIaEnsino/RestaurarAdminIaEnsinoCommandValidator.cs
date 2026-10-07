using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Commands.RestaurarAdminIaEnsino;

/// <summary>
/// Validates AI teaching restoration.
/// </summary>
public sealed class RestaurarAdminIaEnsinoCommandValidator : AbstractValidator<RestaurarAdminIaEnsinoCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public RestaurarAdminIaEnsinoCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.AdminUsuarioId).NotEmpty();
        RuleFor(command => command.Request.Versao)
            .NotEmpty()
            .MaximumLength(40)
            .Matches("^[A-Za-z0-9._:-]+$")
            .WithMessage("Informe uma versao valida para restaurar.");
        RuleFor(command => command.Request.Observacao).MaximumLength(500);
    }
}
