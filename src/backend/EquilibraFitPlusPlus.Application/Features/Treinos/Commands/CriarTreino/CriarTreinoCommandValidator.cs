using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.CriarTreino;

/// <summary>
/// Validates workout creation.
/// </summary>
public sealed class CriarTreinoCommandValidator : AbstractValidator<CriarTreinoCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public CriarTreinoCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.UsuarioId).NotEmpty();
        RuleFor(command => command.Request.Nome).NotEmpty().MaximumLength(160);
        RuleFor(command => command.Request.Objetivo).NotEmpty().MaximumLength(160);
        RuleFor(command => command.Request.FrequenciaSemanal).InclusiveBetween((byte)1, (byte)7);
        RuleFor(command => command.Request.DuracaoSemanas)
            .Must(value => !value.HasValue || value.Value is >= 1 and <= 52)
            .WithMessage("A duração do ciclo deve ficar entre 1 e 52 semanas.");
        RuleFor(command => command.Request.Fase).MaximumLength(80);
        RuleFor(command => command.Request.Exercicios).NotEmpty().Must(items => items.Count <= 60).WithMessage("Um treino pode conter até 60 exercícios.");
        RuleFor(command => command.Request.Exercicios)
            .Must(items => items.Select(item => (item.DiaTreino ?? 1, item.Ordem)).Distinct().Count() == items.Count)
            .WithMessage("A ordem dos exercícios não pode se repetir no mesmo dia.");

        RuleForEach(command => command.Request.Exercicios).ChildRules(item =>
        {
            item.RuleFor(x => x.Ordem).GreaterThan(0).LessThanOrEqualTo(200);
            item.RuleFor(x => x.DiaTreino)
                .Must(value => !value.HasValue || value.Value is >= 1 and <= 7)
                .WithMessage("Informe um dia de treino entre 1 e 7.");
            item.RuleFor(x => x.Series).InclusiveBetween(1, 20);
            item.RuleFor(x => x.Repeticoes).NotEmpty().MaximumLength(40);
            item.RuleFor(x => x.DescansoSegundos).InclusiveBetween(0, 900);
            item.RuleFor(x => x.CargaAlvoKg)
                .Must(value => !value.HasValue || value.Value is >= 0 and <= 1000)
                .WithMessage("Informe uma carga alvo entre 0 e 1000 kg.");
            item.RuleFor(x => x.RpeAlvo)
                .Must(value => !value.HasValue || value.Value is >= 1 and <= 10)
                .WithMessage("Informe um RPE alvo entre 1 e 10.");
            item.RuleFor(x => x.RepeticoesMin)
                .Must(value => !value.HasValue || value.Value is >= 0 and <= 200)
                .WithMessage("Informe repetições mínimas entre 0 e 200.");
            item.RuleFor(x => x.RepeticoesMax)
                .Must(value => !value.HasValue || value.Value is >= 0 and <= 200)
                .WithMessage("Informe repetições máximas entre 0 e 200.");
            item.RuleFor(x => x)
                .Must(x => !x.RepeticoesMin.HasValue || !x.RepeticoesMax.HasValue || x.RepeticoesMin <= x.RepeticoesMax)
                .WithMessage("A faixa de repetições deve manter o mínimo menor ou igual ao máximo.");
            item.RuleFor(x => x.Nome).MaximumLength(160);
            item.RuleFor(x => x.GrupoMuscular).MaximumLength(80);
            item.RuleFor(x => x.Nivel).MaximumLength(40);
            item.RuleFor(x => x.Equipamento).MaximumLength(120);
            item.RuleFor(x => x.Instrucao).MaximumLength(4000);
            item.RuleFor(x => x.Observacao).MaximumLength(1000);
            item.RuleFor(x => x.ProgressaoMotivo).MaximumLength(1000);
            item.RuleFor(x => x)
                .Must(x => x.ExercicioId.HasValue || (!string.IsNullOrWhiteSpace(x.Nome) && !string.IsNullOrWhiteSpace(x.GrupoMuscular) && !string.IsNullOrWhiteSpace(x.Instrucao)))
                .WithMessage("Informe um ExercicioId existente ou os dados do novo exercício.");
        });
    }
}
