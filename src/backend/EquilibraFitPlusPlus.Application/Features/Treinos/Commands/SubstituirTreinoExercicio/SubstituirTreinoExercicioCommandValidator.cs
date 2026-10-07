using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.SubstituirTreinoExercicio;

/// <summary>
/// Validates prescribed exercise replacement.
/// </summary>
public sealed class SubstituirTreinoExercicioCommandValidator : AbstractValidator<SubstituirTreinoExercicioCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public SubstituirTreinoExercicioCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.UsuarioId).NotEmpty();
        RuleFor(command => command.TreinoId).NotEmpty();
        RuleFor(command => command.TreinoExercicioId).NotEmpty();
        RuleFor(command => command.Request.Nome).MaximumLength(160);
        RuleFor(command => command.Request.GrupoMuscular).MaximumLength(80);
        RuleFor(command => command.Request.Nivel).MaximumLength(40);
        RuleFor(command => command.Request.Equipamento).MaximumLength(120);
        RuleFor(command => command.Request.Instrucao).MaximumLength(4000);
        RuleFor(command => command.Request)
            .Must(request =>
                request.NovoExercicioId.HasValue
                || request.UsarSugestaoIa
                || (!string.IsNullOrWhiteSpace(request.Nome)
                    && !string.IsNullOrWhiteSpace(request.GrupoMuscular)
                    && !string.IsNullOrWhiteSpace(request.Instrucao)))
            .WithMessage("Escolha um exercício, solicite sugestão ou informe os dados manuais.");
    }
}
