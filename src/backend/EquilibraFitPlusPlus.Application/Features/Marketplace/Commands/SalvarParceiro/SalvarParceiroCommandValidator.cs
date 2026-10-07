using EquilibraFitPlusPlus.Domain.Enums;
using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Marketplace.Commands.SalvarParceiro;

/// <summary>
/// Validates partner commands.
/// </summary>
public sealed class SalvarParceiroCommandValidator : AbstractValidator<SalvarParceiroCommand>
{
    /// <summary>Initializes validation rules.</summary>
    public SalvarParceiroCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.UsuarioId).NotEmpty();
        RuleFor(command => command.Request.Nome).NotEmpty().MaximumLength(160);
        RuleFor(command => command.Request.Tipo).Must(value => Enum.TryParse<TipoParceiro>(value, true, out _)).WithMessage("Tipo de parceiro inválido.");
        RuleFor(command => command.Request.Status).Must(value => Enum.TryParse<StatusParceiro>(value, true, out _)).WithMessage("Status de parceiro inválido.");
        RuleFor(command => command.Request.EmailContato).EmailAddress().MaximumLength(256).When(command => !string.IsNullOrWhiteSpace(command.Request.EmailContato));
        RuleFor(command => command.Request.Documento).MaximumLength(32);
    }
}
