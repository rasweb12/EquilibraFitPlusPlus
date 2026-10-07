using EquilibraFitPlusPlus.Application.Abstractions.Admin;
using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Notificacoes;
using EquilibraFitPlusPlus.Application.Abstractions.Users;
using EquilibraFitPlusPlus.Contracts.Notificacoes;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Domain.Enums;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Notificacoes.Commands.CriarNotificacao;

/// <summary>
/// Handles notification creation.
/// </summary>
public sealed class CriarNotificacaoCommandHandler : IRequestHandler<CriarNotificacaoCommand, Result<NotificacaoResponse>>
{
    private readonly INotificacaoRepository _notificacaoRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IAdminRepository _adminRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes the handler.</summary>
    public CriarNotificacaoCommandHandler(
        INotificacaoRepository notificacaoRepository,
        IUsuarioRepository usuarioRepository,
        IAdminRepository adminRepository,
        IUnitOfWork unitOfWork)
    {
        _notificacaoRepository = notificacaoRepository;
        _usuarioRepository = usuarioRepository;
        _adminRepository = adminRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<NotificacaoResponse>> Handle(CriarNotificacaoCommand command, CancellationToken cancellationToken)
    {
        if (await _usuarioRepository.ObterAtivoAsync(command.TenantId, command.Request.UsuarioId, cancellationToken) is null)
        {
            return Result<NotificacaoResponse>.Failure(new Error("usuario.nao_encontrado", "Não foi possível localizar o usuário para notificar."));
        }

        var notificacao = new Notificacao
        {
            TenantId = command.TenantId,
            UsuarioId = command.Request.UsuarioId,
            Titulo = command.Request.Titulo.Trim(),
            Mensagem = command.Request.Mensagem.Trim(),
            Status = StatusNotificacao.Pendente
        };

        _notificacaoRepository.Adicionar(notificacao);
        _adminRepository.AdicionarAuditoria(new Auditoria
        {
            TenantId = command.TenantId,
            UsuarioId = command.UsuarioAdminId,
            Acao = "notificacao.criar",
            Entidade = nameof(Notificacao),
            EntidadeId = notificacao.Id,
            Ip = command.Ip,
            UserAgent = command.UserAgent
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<NotificacaoResponse>.Success(NotificacaoMapper.Map(notificacao));
    }
}
