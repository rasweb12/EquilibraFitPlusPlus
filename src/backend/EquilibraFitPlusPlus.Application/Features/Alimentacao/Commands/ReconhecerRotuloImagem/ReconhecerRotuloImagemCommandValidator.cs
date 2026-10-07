using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Alimentacao.Commands.ReconhecerRotuloImagem;

/// <summary>
/// Validates nutrition label image recognition requests.
/// </summary>
public sealed class ReconhecerRotuloImagemCommandValidator : AbstractValidator<ReconhecerRotuloImagemCommand>
{
    private const int MaxBase64Length = 11_200_000;

    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public ReconhecerRotuloImagemCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.UsuarioId).NotEmpty();
        RuleFor(command => command.Request.ImageBase64)
            .NotEmpty()
            .MaximumLength(MaxBase64Length)
            .Must(BeBase64)
            .WithMessage("Envie uma imagem válida de até 8 MB para reconhecimento.");
        RuleFor(command => command.Request.Contexto).MaximumLength(500);
    }

    private static bool BeBase64(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        Span<byte> buffer = new byte[value.Length];
        return Convert.TryFromBase64String(value, buffer, out _);
    }
}
