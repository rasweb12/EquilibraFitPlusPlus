using EquilibraFitPlusPlus.Application.Abstractions.Alimentacao;
using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Alimentacao.Commands.ExcluirRegistroAlimentar;

/// <summary>
/// Handles food log soft deletion.
/// </summary>
public sealed class ExcluirRegistroAlimentarCommandHandler : IRequestHandler<ExcluirRegistroAlimentarCommand, Result>
{
    private readonly IAlimentacaoRepository _alimentacaoRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public ExcluirRegistroAlimentarCommandHandler(IAlimentacaoRepository alimentacaoRepository, IUnitOfWork unitOfWork)
    {
        _alimentacaoRepository = alimentacaoRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ExcluirRegistroAlimentarCommand request, CancellationToken cancellationToken)
    {
        RegistroAlimentar? registro = await _alimentacaoRepository.ObterPorIdAsync(request.TenantId, request.UsuarioId, request.RegistroId, cancellationToken);
        if (registro is null)
        {
            return Result.Failure(new Error("alimentacao.nao_encontrada", "Não encontramos esse registro alimentar."));
        }

        registro.ExcluidoEm = DateTimeOffset.UtcNow;
        registro.ExcluidoPor = request.UsuarioId;
        registro.MotivoExclusao = "Exclusão solicitada pelo usuário.";

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
