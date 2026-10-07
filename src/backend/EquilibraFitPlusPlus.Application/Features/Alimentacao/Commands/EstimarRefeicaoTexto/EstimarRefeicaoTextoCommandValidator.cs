using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Alimentacao.Commands.EstimarRefeicaoTexto;

/// <summary>
/// Validates text meal estimation requests.
/// </summary>
public sealed class EstimarRefeicaoTextoCommandValidator : AbstractValidator<EstimarRefeicaoTextoCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public EstimarRefeicaoTextoCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.UsuarioId).NotEmpty();
        RuleFor(command => command.Request.Descricao)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(600)
            .WithMessage("Descreva a refeição com algumas palavras para estimarmos melhor.");
        RuleFor(command => command.Request.TipoRefeicao).MaximumLength(40);
    }
}
