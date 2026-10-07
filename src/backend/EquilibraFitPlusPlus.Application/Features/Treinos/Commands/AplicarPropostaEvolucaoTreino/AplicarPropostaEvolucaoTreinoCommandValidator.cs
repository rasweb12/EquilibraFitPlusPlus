using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.AplicarPropostaEvolucaoTreino;

/// <summary>
/// Validates workout evolution proposal application.
/// </summary>
public sealed class AplicarPropostaEvolucaoTreinoCommandValidator : AbstractValidator<AplicarPropostaEvolucaoTreinoCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public AplicarPropostaEvolucaoTreinoCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.UsuarioId).NotEmpty();
        RuleFor(command => command.TreinoId).NotEmpty();
        RuleFor(command => command.PropostaId).NotEmpty();
    }
}
