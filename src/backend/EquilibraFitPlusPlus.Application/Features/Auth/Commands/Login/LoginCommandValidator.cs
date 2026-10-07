using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Auth.Commands.Login;

/// <summary>
/// Validates login commands.
/// </summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public LoginCommandValidator()
    {
        RuleFor(command => command.Request.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(command => command.Request.Senha).NotEmpty().MaximumLength(256);
    }
}
