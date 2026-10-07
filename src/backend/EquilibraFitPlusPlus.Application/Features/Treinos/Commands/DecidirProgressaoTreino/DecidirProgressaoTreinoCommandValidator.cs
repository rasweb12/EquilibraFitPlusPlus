using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.DecidirProgressaoTreino;

/// <summary>
/// Validates progression decisions.
/// </summary>
public sealed class DecidirProgressaoTreinoCommandValidator : AbstractValidator<DecidirProgressaoTreinoCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public DecidirProgressaoTreinoCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.UsuarioId).NotEmpty();
        RuleFor(command => command.SugestaoId).NotEmpty();
    }
}
