using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.AiCoach.Commands.EnviarMensagemCoach;

/// <summary>
/// Validates AI Coach message commands.
/// </summary>
public sealed class EnviarMensagemCoachCommandValidator : AbstractValidator<EnviarMensagemCoachCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public EnviarMensagemCoachCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.UsuarioId).NotEmpty();
        RuleFor(command => command.Request.Mensagem)
            .NotEmpty()
            .MaximumLength(2000)
            .WithMessage("Envie uma mensagem de até 2000 caracteres para o Coach IA.");
        RuleFor(command => command.Request.Provedor)
            .Must(provider => provider is null or "openai" or "gemini")
            .WithMessage("Escolha OpenAI ou Gemini para o Coach IA.");
    }
}
