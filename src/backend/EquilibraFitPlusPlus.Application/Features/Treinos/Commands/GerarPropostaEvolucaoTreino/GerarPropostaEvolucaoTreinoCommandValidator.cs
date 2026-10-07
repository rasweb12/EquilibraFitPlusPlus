using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.GerarPropostaEvolucaoTreino;

/// <summary>
/// Validates workout evolution proposal requests.
/// </summary>
public sealed class GerarPropostaEvolucaoTreinoCommandValidator : AbstractValidator<GerarPropostaEvolucaoTreinoCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public GerarPropostaEvolucaoTreinoCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.UsuarioId).NotEmpty();
        RuleFor(command => command.TreinoId).NotEmpty();
        RuleFor(command => command.Request.Observacao).MaximumLength(1000);
    }
}
