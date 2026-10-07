using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.SugerirProgressaoTreino;

/// <summary>
/// Validates progression suggestion requests.
/// </summary>
public sealed class SugerirProgressaoTreinoCommandValidator : AbstractValidator<SugerirProgressaoTreinoCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public SugerirProgressaoTreinoCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.UsuarioId).NotEmpty();
        RuleFor(command => command.TreinoId).NotEmpty();
    }
}
