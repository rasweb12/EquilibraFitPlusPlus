using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Notificacoes;
using EquilibraFitPlusPlus.Contracts.Notificacoes;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Domain.Enums;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Notificacoes.Commands.MarcarNotificacaoLida;

/// <summary>
/// Handles notification read status.
/// </summary>
public sealed class MarcarNotificacaoLidaCommandHandler : IRequestHandler<MarcarNotificacaoLidaCommand, Result<NotificacaoResponse>>
{
    private readonly INotificacaoRepository _notificacaoRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes the handler.</summary>
    public MarcarNotificacaoLidaCommandHandler(INotificacaoRepository notificacaoRepository, IUnitOfWork unitOfWork)
    {
        _notificacaoRepository = notificacaoRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<NotificacaoResponse>> Handle(MarcarNotificacaoLidaCommand command, CancellationToken cancellationToken)
    {
        Notificacao? notificacao = await _notificacaoRepository.ObterUsuarioNotificacaoAsync(command.TenantId, command.UsuarioId, command.NotificacaoId, cancellationToken);
        if (notificacao is null)
        {
            return Result<NotificacaoResponse>.Failure(new Error("notificacao.nao_encontrada", "Não encontramos essa notificação."));
        }

        notificacao.Status = StatusNotificacao.Lida;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<NotificacaoResponse>.Success(NotificacaoMapper.Map(notificacao));
    }
}
