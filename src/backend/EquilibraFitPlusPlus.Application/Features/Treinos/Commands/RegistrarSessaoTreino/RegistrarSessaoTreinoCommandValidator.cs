using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.RegistrarSessaoTreino;

/// <summary>
/// Validates workout execution sessions.
/// </summary>
public sealed class RegistrarSessaoTreinoCommandValidator : AbstractValidator<RegistrarSessaoTreinoCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public RegistrarSessaoTreinoCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.UsuarioId).NotEmpty();
        RuleFor(command => command.TreinoId).NotEmpty();
        RuleFor(command => command.Request.DiaTreino).InclusiveBetween((byte)1, (byte)7);
        RuleFor(command => command.Request.Observacao).MaximumLength(1000);
        RuleFor(command => command.Request.Series).NotEmpty().Must(items => items.Count <= 200).WithMessage("Uma sessão pode registrar até 200 séries.");
        RuleForEach(command => command.Request.Series).ChildRules(serie =>
        {
            serie.RuleFor(item => item.TreinoExercicioId).NotEmpty();
            serie.RuleFor(item => item.NumeroSerie).InclusiveBetween(1, 20);
            serie.RuleFor(item => item.CargaKg).Must(value => !value.HasValue || value.Value is >= 0 and <= 1000).WithMessage("Informe uma carga entre 0 e 1000 kg.");
            serie.RuleFor(item => item.RepeticoesRealizadas).InclusiveBetween(0, 200);
            serie.RuleFor(item => item.Rpe).InclusiveBetween((byte)1, (byte)10);
            serie.RuleFor(item => item.Observacao).MaximumLength(1000);
            serie.RuleFor(item => item.DorDescricao).MaximumLength(1000);
        });
    }
}
