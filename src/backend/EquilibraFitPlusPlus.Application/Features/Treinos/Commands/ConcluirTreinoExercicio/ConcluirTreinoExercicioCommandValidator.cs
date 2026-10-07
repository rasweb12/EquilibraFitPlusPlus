using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.ConcluirTreinoExercicio;

/// <summary>
/// Validates workout exercise completion updates.
/// </summary>
public sealed class ConcluirTreinoExercicioCommandValidator : AbstractValidator<ConcluirTreinoExercicioCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public ConcluirTreinoExercicioCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.UsuarioId).NotEmpty();
        RuleFor(command => command.TreinoId).NotEmpty();
        RuleFor(command => command.TreinoExercicioId).NotEmpty();
        RuleFor(command => command.Request.Data)
            .Must(data => !data.HasValue || data.Value <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage("A data do treino deve estar dentro de um período seguro.");
    }
}
