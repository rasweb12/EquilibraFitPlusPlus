using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Lgpd.Commands.SolicitarExclusaoLgpd;

/// <summary>
/// Validates LGPD deletion requests.
/// </summary>
public sealed class SolicitarExclusaoLgpdCommandValidator : AbstractValidator<SolicitarExclusaoLgpdCommand>
{
    /// <summary>Initializes validation rules.</summary>
    public SolicitarExclusaoLgpdCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.UsuarioId).NotEmpty();
        RuleFor(command => command.Request.Confirmacao)
            .Equal("EXCLUIR")
            .WithMessage("Para solicitar exclusao, envie a confirmacao EXCLUIR.");
    }
}
