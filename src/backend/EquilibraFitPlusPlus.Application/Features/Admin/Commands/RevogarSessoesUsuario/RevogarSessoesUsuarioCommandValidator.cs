using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Commands.RevogarSessoesUsuario;

/// <summary>
/// Validates user session revocation commands.
/// </summary>
public sealed class RevogarSessoesUsuarioCommandValidator : AbstractValidator<RevogarSessoesUsuarioCommand>
{
    /// <summary>Initializes validation rules.</summary>
    public RevogarSessoesUsuarioCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.AdminUsuarioId).NotEmpty();
        RuleFor(command => command.UsuarioId).NotEmpty();
    }
}
