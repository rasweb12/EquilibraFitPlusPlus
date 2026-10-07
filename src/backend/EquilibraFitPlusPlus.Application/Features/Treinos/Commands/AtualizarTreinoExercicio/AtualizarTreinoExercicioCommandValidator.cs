using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.AtualizarTreinoExercicio;

/// <summary>
/// Validates prescribed exercise updates.
/// </summary>
public sealed class AtualizarTreinoExercicioCommandValidator : AbstractValidator<AtualizarTreinoExercicioCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public AtualizarTreinoExercicioCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.UsuarioId).NotEmpty();
        RuleFor(command => command.TreinoId).NotEmpty();
        RuleFor(command => command.TreinoExercicioId).NotEmpty();
        RuleFor(command => command.Request.DiaTreino).InclusiveBetween((byte)1, (byte)7);
        RuleFor(command => command.Request.Ordem).InclusiveBetween(1, 200);
        RuleFor(command => command.Request.Series).InclusiveBetween(1, 20);
        RuleFor(command => command.Request.Repeticoes).NotEmpty().MaximumLength(40);
        RuleFor(command => command.Request.DescansoSegundos).InclusiveBetween(0, 900);
        RuleFor(command => command.Request.Equipamento).MaximumLength(120);
        RuleFor(command => command.Request.Observacao).MaximumLength(1000);
        RuleFor(command => command.Request.CargaAlvoKg)
            .Must(value => !value.HasValue || value.Value is >= 0 and <= 1000)
            .WithMessage("Informe uma carga alvo entre 0 e 1000 kg.");
        RuleFor(command => command.Request.RpeAlvo)
            .Must(value => !value.HasValue || value.Value is >= 1 and <= 10)
            .WithMessage("Informe um RPE alvo entre 1 e 10.");
        RuleFor(command => command.Request.RepeticoesMin)
            .Must(value => !value.HasValue || value.Value is >= 0 and <= 200)
            .WithMessage("Informe repetições mínimas entre 0 e 200.");
        RuleFor(command => command.Request.RepeticoesMax)
            .Must(value => !value.HasValue || value.Value is >= 0 and <= 200)
            .WithMessage("Informe repetições máximas entre 0 e 200.");
        RuleFor(command => command.Request)
            .Must(request => !request.RepeticoesMin.HasValue || !request.RepeticoesMax.HasValue || request.RepeticoesMin <= request.RepeticoesMax)
            .WithMessage("A faixa de repetições deve manter o mínimo menor ou igual ao máximo.");
        RuleFor(command => command.Request.ProgressaoMotivo).MaximumLength(1000);
    }
}
