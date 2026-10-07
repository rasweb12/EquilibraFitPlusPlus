using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Validates refresh token commands.
/// </summary>
public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public RefreshTokenCommandValidator()
    {
        RuleFor(command => command.Request.RefreshToken).NotEmpty().MaximumLength(4096);
    }
}
