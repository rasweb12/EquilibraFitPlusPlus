using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Treinos;
using EquilibraFitPlusPlus.Application.Features.Treinos.Mappings;
using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.SubstituirTreinoExercicio;

/// <summary>
/// Handles prescribed exercise replacement.
/// </summary>
public sealed class SubstituirTreinoExercicioCommandHandler : IRequestHandler<SubstituirTreinoExercicioCommand, Result<TreinoUsuarioResponse>>
{
    private readonly ITreinoRepository _treinoRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public SubstituirTreinoExercicioCommandHandler(ITreinoRepository treinoRepository, IUnitOfWork unitOfWork)
    {
        _treinoRepository = treinoRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<TreinoUsuarioResponse>> Handle(SubstituirTreinoExercicioCommand command, CancellationToken cancellationToken)
    {
        TreinoExercicio? prescribed = await _treinoRepository.ObterTreinoExercicioAsync(
            command.TenantId,
            command.UsuarioId,
            command.TreinoId,
            command.TreinoExercicioId,
            cancellationToken);

        if (prescribed?.TreinoUsuario is null || prescribed.Exercicio is null)
        {
            return Result<TreinoUsuarioResponse>.Failure(new Error("treinos.exercicio_nao_encontrado", "Exercício não encontrado neste treino. Podemos atualizar a tela e tentar novamente."));
        }

        Exercicio replacement;
        bool shouldAddReplacement = false;
        if (command.Request.NovoExercicioId.HasValue)
        {
            IReadOnlyDictionary<Guid, Exercicio> exercises = await _treinoRepository.ObterExerciciosPorIdsAsync(command.TenantId, [command.Request.NovoExercicioId.Value], cancellationToken);
            if (!exercises.TryGetValue(command.Request.NovoExercicioId.Value, out replacement!))
            {
                return Result<TreinoUsuarioResponse>.Failure(new Error("treinos.exercicio_substituto_nao_encontrado", "Não encontramos o exercício escolhido. Podemos atualizar a busca e tentar novamente."));
            }
        }
        else if (command.Request.UsarSugestaoIa)
        {
            IReadOnlyCollection<Exercicio> candidates = await _treinoRepository.ListarSubstitutosAsync(
                command.TenantId,
                prescribed.ExercicioId,
                prescribed.Exercicio.GrupoMuscular,
                prescribed.Exercicio.Equipamento,
                1,
                cancellationToken);

            replacement = candidates.FirstOrDefault() ?? CreateSuggestedExercise(command.TenantId, prescribed.Exercicio);
            shouldAddReplacement = !candidates.Any();
        }
        else
        {
            replacement = new Exercicio
            {
                TenantId = command.TenantId,
                Nome = command.Request.Nome!.Trim(),
                GrupoMuscular = command.Request.GrupoMuscular!.Trim(),
                Nivel = string.IsNullOrWhiteSpace(command.Request.Nivel) ? prescribed.Exercicio.Nivel : command.Request.Nivel.Trim(),
                Equipamento = string.IsNullOrWhiteSpace(command.Request.Equipamento) ? prescribed.Exercicio.Equipamento : command.Request.Equipamento.Trim(),
                Instrucao = command.Request.Instrucao!.Trim()
            };
            shouldAddReplacement = true;
        }

        if (shouldAddReplacement)
        {
            _treinoRepository.AdicionarExercicio(replacement);
        }

        prescribed.Exercicio = replacement;
        prescribed.ExercicioId = replacement.Id;
        prescribed.Observacao = $"Substituído preservando foco em {replacement.GrupoMuscular}.";

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        TreinoUsuario treino = await _treinoRepository.ObterTreinoPorIdAsync(command.TenantId, command.UsuarioId, command.TreinoId, cancellationToken)
            ?? prescribed.TreinoUsuario;
        return Result<TreinoUsuarioResponse>.Success(TreinoMapper.Map(treino));
    }

    private static Exercicio CreateSuggestedExercise(Guid tenantId, Exercicio current)
    {
        return new Exercicio
        {
            TenantId = tenantId,
            Nome = $"Alternativa segura para {current.Nome}",
            GrupoMuscular = current.GrupoMuscular,
            Nivel = current.Nivel,
            Equipamento = current.Equipamento,
            Instrucao = $"Execute uma variação confortável para {current.GrupoMuscular}, mantendo controle, respiração e sem dor."
        };
    }
}
