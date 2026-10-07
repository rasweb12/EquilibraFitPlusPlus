using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Notificacoes.Commands.CriarNotificacao;

/// <summary>
/// Validates notification creation.
/// </summary>
public sealed class CriarNotificacaoCommandValidator : AbstractValidator<CriarNotificacaoCommand>
{
    /// <summary>Initializes validation rules.</summary>
    public CriarNotificacaoCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.UsuarioAdminId).NotEmpty();
        RuleFor(command => command.Request.UsuarioId).NotEmpty();
        RuleFor(command => command.Request.Titulo).NotEmpty().MaximumLength(160);
        RuleFor(command => command.Request.Mensagem).NotEmpty().MaximumLength(1000);
    }
}
