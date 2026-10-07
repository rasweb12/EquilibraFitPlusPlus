using EquilibraFitPlusPlus.Domain.Enums;
using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Auth.Commands.CadastrarUsuario;

/// <summary>
/// Validates user registration commands.
/// </summary>
public sealed class CadastrarUsuarioCommandValidator : AbstractValidator<CadastrarUsuarioCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public CadastrarUsuarioCommandValidator()
    {
        RuleFor(command => command.Request.Nome).NotEmpty().MaximumLength(160);
        RuleFor(command => command.Request.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(command => command.Request.Senha)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"\p{Lu}").WithMessage("A senha deve conter ao menos uma letra maiuscula.")
            .Matches(@"\p{Ll}").WithMessage("A senha deve conter ao menos uma letra minuscula.")
            .Matches("[0-9]").WithMessage("A senha deve conter ao menos um numero.");

        RuleFor(command => command.Request.Aceites)
            .NotEmpty()
            .Must(aceites => aceites.Any(a => a.Tipo == TipoConsentimento.TermosUso.ToString()))
            .WithMessage("O aceite dos termos de uso e obrigatorio.")
            .Must(aceites => aceites.Any(a => a.Tipo == TipoConsentimento.Privacidade.ToString()))
            .WithMessage("O aceite da politica de privacidade e obrigatorio.");

        RuleForEach(command => command.Request.Aceites).ChildRules(consentimento =>
        {
            consentimento.RuleFor(a => a.Tipo).NotEmpty().MaximumLength(80);
            consentimento.RuleFor(a => a.Versao).NotEmpty().MaximumLength(40);
        });
    }
}
