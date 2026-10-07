using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Treinos;
using EquilibraFitPlusPlus.Application.Features.Treinos.Mappings;
using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.RegistrarSessaoTreino;

/// <summary>
/// Handles workout execution session registration.
/// </summary>
public sealed class RegistrarSessaoTreinoCommandHandler : IRequestHandler<RegistrarSessaoTreinoCommand, Result<TreinoSessaoResponse>>
{
    private readonly ITreinoRepository _treinoRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public RegistrarSessaoTreinoCommandHandler(ITreinoRepository treinoRepository, IUnitOfWork unitOfWork)
    {
        _treinoRepository = treinoRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<TreinoSessaoResponse>> Handle(RegistrarSessaoTreinoCommand command, CancellationToken cancellationToken)
    {
        if (command.Request.OperationId is Guid operationId)
        {
            TreinoSessao? existingSession = await _treinoRepository.ObterSessaoPorOperacaoAsync(
                command.TenantId,
                command.UsuarioId,
                operationId,
                cancellationToken);

            if (existingSession is not null)
            {
                return Result<TreinoSessaoResponse>.Success(TreinoMapper.Map(existingSession));
            }
        }

        TreinoUsuario? treino = await _treinoRepository.ObterTreinoPorIdAsync(command.TenantId, command.UsuarioId, command.TreinoId, cancellationToken);
        if (treino is null)
        {
            return Result<TreinoSessaoResponse>.Failure(new Error("treinos.nao_encontrado", "Treino não encontrado. Podemos atualizar a tela e tentar novamente."));
        }

        Dictionary<Guid, TreinoExercicio> prescribedById = treino.Exercicios.ToDictionary(item => item.Id);
        Guid[] invalidExerciseIds = command.Request.Series
            .Select(serie => serie.TreinoExercicioId)
            .Where(id => !prescribedById.ContainsKey(id))
            .Distinct()
            .ToArray();

        if (invalidExerciseIds.Length > 0)
        {
            return Result<TreinoSessaoResponse>.Failure(new Error("treinos.exercicio_nao_encontrado", "Uma das séries pertence a outro treino. Podemos atualizar a tela e tentar novamente."));
        }

        DateOnly data = command.Request.Data ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var sessao = new TreinoSessao
        {
            TenantId = command.TenantId,
            UsuarioId = command.UsuarioId,
            TreinoUsuarioId = treino.Id,
            TreinoUsuario = treino,
            OperationId = command.Request.OperationId,
            DiaTreino = command.Request.DiaTreino,
            Data = data,
            IniciadoEm = DateTimeOffset.UtcNow,
            FinalizadoEm = DateTimeOffset.UtcNow,
            Observacao = string.IsNullOrWhiteSpace(command.Request.Observacao) ? null : command.Request.Observacao.Trim()
        };

        foreach (RegistrarSerieTreinoRequest item in command.Request.Series.OrderBy(item => item.TreinoExercicioId).ThenBy(item => item.NumeroSerie))
        {
            sessao.Series.Add(new TreinoSerieRealizada
            {
                TenantId = command.TenantId,
                TreinoSessaoId = sessao.Id,
                TreinoExercicioId = item.TreinoExercicioId,
                NumeroSerie = item.NumeroSerie,
                CargaKg = item.CargaKg,
                RepeticoesRealizadas = item.RepeticoesRealizadas,
                Rpe = item.Rpe,
                Observacao = string.IsNullOrWhiteSpace(item.Observacao) ? null : item.Observacao.Trim(),
                DorDesconforto = item.DorDesconforto,
                DorDescricao = string.IsNullOrWhiteSpace(item.DorDescricao) ? null : item.DorDescricao.Trim()
            });
        }

        sessao.Resumo = CreateSummary(sessao.Series);
        _treinoRepository.AdicionarSessao(sessao);

        foreach (Guid treinoExercicioId in command.Request.Series.Select(item => item.TreinoExercicioId).Distinct())
        {
            TreinoExercicioConclusao? conclusao = await _treinoRepository.ObterConclusaoAsync(
                command.TenantId,
                command.UsuarioId,
                treinoExercicioId,
                data,
                cancellationToken);

            if (conclusao is null)
            {
                conclusao = new TreinoExercicioConclusao
                {
                    TenantId = command.TenantId,
                    UsuarioId = command.UsuarioId,
                    TreinoUsuarioId = treino.Id,
                    TreinoExercicioId = treinoExercicioId,
                    Data = data
                };
                _treinoRepository.AdicionarConclusao(conclusao);
            }

            conclusao.Concluido = true;
            conclusao.ConcluidoEm = DateTimeOffset.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<TreinoSessaoResponse>.Success(TreinoMapper.Map(sessao));
    }

    private static string CreateSummary(IEnumerable<TreinoSerieRealizada> series)
    {
        TreinoSerieRealizada[] performedSets = series.ToArray();
        int totalSets = performedSets.Length;
        int totalRepetitions = performedSets.Sum(serie => serie.RepeticoesRealizadas);
        decimal volume = performedSets.Sum(serie => (serie.CargaKg ?? 0) * serie.RepeticoesRealizadas);
        bool painReported = performedSets.Any(serie => serie.DorDesconforto);

        string painMessage = painReported
            ? " Você registrou desconforto; reduza intensidade e procure orientação profissional se persistir."
            : string.Empty;

        return $"Sessão registrada com {totalSets} séries, {totalRepetitions} repetições e volume estimado de {volume:0.##} kg.{painMessage}";
    }
}
