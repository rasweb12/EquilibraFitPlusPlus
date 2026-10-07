using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.AtualizarTreino;

/// <summary>
/// Validates workout metadata updates.
/// </summary>
public sealed class AtualizarTreinoCommandValidator : AbstractValidator<AtualizarTreinoCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public AtualizarTreinoCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.UsuarioId).NotEmpty();
        RuleFor(command => command.TreinoId).NotEmpty();
        RuleFor(command => command.Request.Nome).NotEmpty().MaximumLength(160);
        RuleFor(command => command.Request.Objetivo).NotEmpty().MaximumLength(160);
        RuleFor(command => command.Request.FrequenciaSemanal).InclusiveBetween((byte)1, (byte)7);
        RuleFor(command => command.Request.DuracaoSemanas).InclusiveBetween(1, 52);
        RuleFor(command => command.Request.Fase).NotEmpty().MaximumLength(80);
    }
}
