using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Treinos;
using EquilibraFitPlusPlus.Application.Features.Treinos.Mappings;
using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.DecidirProgressaoTreino;

/// <summary>
/// Handles progression decisions.
/// </summary>
public sealed class DecidirProgressaoTreinoCommandHandler : IRequestHandler<DecidirProgressaoTreinoCommand, Result<TreinoProgressaoSugestaoResponse>>
{
    private readonly ITreinoRepository _treinoRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public DecidirProgressaoTreinoCommandHandler(ITreinoRepository treinoRepository, IUnitOfWork unitOfWork)
    {
        _treinoRepository = treinoRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<TreinoProgressaoSugestaoResponse>> Handle(DecidirProgressaoTreinoCommand command, CancellationToken cancellationToken)
    {
        TreinoProgressaoSugestao? suggestion = await _treinoRepository.ObterProgressaoSugestaoAsync(
            command.TenantId,
            command.UsuarioId,
            command.SugestaoId,
            cancellationToken);

        if (suggestion is null)
        {
            return Result<TreinoProgressaoSugestaoResponse>.Failure(new Error("treinos.progressao_nao_encontrada", "Sugestão não encontrada. Podemos atualizar a tela e tentar novamente."));
        }

        if (!suggestion.Status.Equals("Pendente", StringComparison.OrdinalIgnoreCase))
        {
            return Result<TreinoProgressaoSugestaoResponse>.Failure(new Error("treinos.progressao_ja_decidida", "Essa sugestão já foi decidida."));
        }

        suggestion.Status = command.Request.Aplicar ? "Aplicada" : "Mantida";
        suggestion.DecididaEm = DateTimeOffset.UtcNow;

        if (command.Request.Aplicar && suggestion.TreinoExercicio is not null)
        {
            suggestion.TreinoExercicio.CargaAlvoKg = suggestion.CargaSugeridaKg ?? suggestion.TreinoExercicio.CargaAlvoKg;
            suggestion.TreinoExercicio.RpeAlvo = suggestion.RpeAlvo ?? suggestion.TreinoExercicio.RpeAlvo;
            suggestion.TreinoExercicio.RepeticoesMin = suggestion.RepeticoesMin ?? suggestion.TreinoExercicio.RepeticoesMin;
            suggestion.TreinoExercicio.RepeticoesMax = suggestion.RepeticoesMax ?? suggestion.TreinoExercicio.RepeticoesMax;
            suggestion.TreinoExercicio.ProgressaoMotivo = suggestion.Motivo;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<TreinoProgressaoSugestaoResponse>.Success(TreinoMapper.Map(suggestion));
    }
}
