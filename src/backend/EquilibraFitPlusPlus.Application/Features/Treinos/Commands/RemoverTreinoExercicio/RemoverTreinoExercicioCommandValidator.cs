using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.RemoverTreinoExercicio;

public sealed class RemoverTreinoExercicioCommandValidator
    : AbstractValidator<RemoverTreinoExercicioCommand>
{
    public RemoverTreinoExercicioCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty();

        RuleFor(x => x.UsuarioId)
            .NotEmpty();

        RuleFor(x => x.TreinoId)
            .NotEmpty();

        RuleFor(x => x.TreinoExercicioId)
            .NotEmpty();
    }
}
