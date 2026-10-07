using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.AdicionarTreinoExercicio;

public sealed class AdicionarTreinoExercicioCommandValidator
    : AbstractValidator<AdicionarTreinoExercicioCommand>
{
    public AdicionarTreinoExercicioCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty();

        RuleFor(x => x.UsuarioId)
            .NotEmpty();

        RuleFor(x => x.TreinoId)
            .NotEmpty();

        RuleFor(x => x.Request.DiaTreino)
            .InclusiveBetween((byte)1, (byte)7);

        RuleFor(x => x.Request.Ordem)
            .GreaterThan(0);

        RuleFor(x => x.Request.Series)
            .InclusiveBetween(1, 20);

        RuleFor(x => x.Request.Repeticoes)
            .NotEmpty()
            .MaximumLength(30);

        RuleFor(x => x.Request.DescansoSegundos)
            .InclusiveBetween(0, 1800);

        RuleFor(x => x.Request.RpeAlvo)
            .InclusiveBetween((byte)1, (byte)10)
            .When(x => x.Request.RpeAlvo.HasValue);

        RuleFor(x => x.Request.CargaAlvoKg)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Request.CargaAlvoKg.HasValue);

        RuleFor(x => x.Request)
            .Must(request =>
                request.ExercicioId.HasValue ||
                !string.IsNullOrWhiteSpace(request.Nome))
            .WithMessage(
                "Informe um exercício existente ou o nome do novo exercício.");

        RuleFor(x => x.Request)
            .Must(request =>
                !request.RepeticoesMin.HasValue ||
                !request.RepeticoesMax.HasValue ||
                request.RepeticoesMin <= request.RepeticoesMax)
            .WithMessage(
                "A repetição mínima não pode ser maior que a máxima.");
    }
}
